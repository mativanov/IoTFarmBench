from __future__ import annotations

import re
from pathlib import Path

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt


ROOT = Path(__file__).resolve().parents[1]
RESULTS_DIR = ROOT / "tests" / "results"
CHARTS_DIR = ROOT / "docs" / "charts"

PROTOCOLS = ["REST", "GraphQL", "gRPC"]
SCENARIOS = {
    "ingestion": "A",
    "selective-monitoring": "B",
    "heavy-querying": "C",
}
RESULT_PREFIX = {
    "REST": "rest",
    "GraphQL": "graphql",
    "gRPC": "grpc",
}

# Postman values from docs/postman-response-size.md. gRPC values are decoded
# Postman gRPC display sizes, not raw HTTP/2/Protobuf frame captures.
RESPONSE_SIZES_BYTES = {
    "A": {"REST": 463, "GraphQL": 205, "gRPC": 524},
    "B": {"REST": 4674, "GraphQL": 4700, "gRPC": 20000},
    "C": {"REST": 402, "GraphQL": 156, "gRPC": 487},
}


def main() -> None:
    CHARTS_DIR.mkdir(parents=True, exist_ok=True)
    latency, rps = load_500vu_k6_metrics()
    service_cpu, service_ram = load_500vu_docker_metrics()

    grouped_bar(
        latency,
        "p95 latency at 500 VU",
        "p95 latency (ms)",
        CHARTS_DIR / "latency-p95-500vu.png",
    )
    grouped_bar(
        rps,
        "Successful RPS at 500 VU",
        "Successful requests/s",
        CHARTS_DIR / "rps-500vu.png",
    )
    grouped_bar(
        RESPONSE_SIZES_BYTES,
        "Response size by scenario",
        "Bytes",
        CHARTS_DIR / "response-size.png",
        grpc_label="gRPC (Postman decoded)",
    )
    grouped_bar(
        service_cpu,
        "Peak service-container CPU at 500 VU",
        "CPU %",
        CHARTS_DIR / "docker-cpu-500vu.png",
    )
    grouped_bar(
        service_ram,
        "Peak service-container RAM at 500 VU",
        "MiB",
        CHARTS_DIR / "docker-ram-500vu.png",
    )


def load_500vu_k6_metrics() -> tuple[dict[str, dict[str, float]], dict[str, dict[str, float]]]:
    latency = empty_chart_data()
    rps = empty_chart_data()

    for protocol, prefix in RESULT_PREFIX.items():
        for scenario, label in SCENARIOS.items():
            path = RESULTS_DIR / f"{prefix}-{scenario}-500vu.txt"
            if not path.exists():
                continue

            text = read_result_text(path)
            latency[label][protocol] = parse_p95_ms(text)
            rps[label][protocol] = parse_successful_rps(text)

    return latency, rps


def load_500vu_docker_metrics() -> tuple[dict[str, dict[str, float]], dict[str, dict[str, float]]]:
    cpu = empty_chart_data()
    ram = empty_chart_data()
    active_container = {
        "REST": "iotfarmbench-rest",
        "GraphQL": "iotfarmbench-graphql",
        "gRPC": "iotfarmbench-grpc",
    }

    for protocol, prefix in RESULT_PREFIX.items():
        for scenario, label in SCENARIOS.items():
            path = RESULTS_DIR / f"docker-stats-{prefix}-{scenario}-500vu.txt"
            if not path.exists():
                continue

            text = read_result_text(path)
            container = active_container[protocol]
            cpu[label][protocol] = parse_peak_cpu(text, container)
            ram[label][protocol] = parse_peak_ram_mib(text, container)

    return cpu, ram


def empty_chart_data() -> dict[str, dict[str, float]]:
    return {label: {protocol: 0.0 for protocol in PROTOCOLS} for label in SCENARIOS.values()}


def read_result_text(path: Path) -> str:
    raw = path.read_bytes()
    if raw.startswith(b"\xff\xfe") or raw.startswith(b"\xfe\xff"):
        return raw.decode("utf-16", errors="replace")
    return raw.decode("utf-8", errors="replace")


def parse_p95_ms(text: str) -> float:
    metric_line = ""
    for line in text.splitlines():
        if ("http_req_duration" in line or "grpc_req_duration" in line) and "avg=" in line:
            metric_line = line

    match = re.search(r"p\(95\)=\s*([0-9.]+)(ms|s)", metric_line)
    if not match:
        return 0.0

    value = float(match.group(1))
    return value * 1000 if match.group(2) == "s" else value


def parse_successful_rps(text: str) -> float:
    match = re.search(r"successful_requests\.*:\s+\d+\s+([0-9.]+)/s", text)
    if match:
        return float(match.group(1))

    # Older successful runs without the custom counter can still be visualized,
    # but reports should prefer the custom successful_requests metric.
    fallback = re.search(r"(?:http_reqs|iterations)\.*:\s+\d+\s+([0-9.]+)/s", text)
    return float(fallback.group(1)) if fallback else 0.0


def parse_peak_cpu(text: str, container: str) -> float:
    values = []
    for line in text.splitlines():
        if line.startswith(container):
            parts = re.split(r"\s{2,}", line.strip())
            if len(parts) >= 2:
                values.append(float(parts[1].replace("%", "")))
    return max(values, default=0.0)


def parse_peak_ram_mib(text: str, container: str) -> float:
    values = []
    for line in text.splitlines():
        if line.startswith(container):
            parts = re.split(r"\s{2,}", line.strip())
            if len(parts) >= 3:
                values.append(to_mib(parts[2].split("/")[0].strip()))
    return max(values, default=0.0)


def to_mib(value: str) -> float:
    match = re.match(r"([0-9.]+)\s*([KMGT]iB)", value)
    if not match:
        return 0.0

    number = float(match.group(1))
    unit = match.group(2)
    factors = {"KiB": 1 / 1024, "MiB": 1, "GiB": 1024, "TiB": 1024 * 1024}
    return number * factors[unit]


def grouped_bar(
    data: dict[str, dict[str, float]],
    title: str,
    ylabel: str,
    output: Path,
    grpc_label: str = "gRPC",
) -> None:
    labels = list(data.keys())
    x = range(len(labels))
    width = 0.24
    colors = {"REST": "#2563eb", "GraphQL": "#c026d3", "gRPC": "#059669"}

    plt.figure(figsize=(9, 5.2), dpi=160)
    for index, protocol in enumerate(PROTOCOLS):
        offset = (index - 1) * width
        legend_label = grpc_label if protocol == "gRPC" else protocol
        values = [data[label].get(protocol, 0.0) for label in labels]
        bars = plt.bar(
            [position + offset for position in x],
            values,
            width=width,
            label=legend_label,
            color=colors[protocol],
        )
        plt.bar_label(bars, fmt=format_value, padding=3, fontsize=8)

    plt.title(title)
    plt.ylabel(ylabel)
    plt.xticks(list(x), [f"Scenario {label}" for label in labels])
    plt.grid(axis="y", alpha=0.25)
    plt.legend()
    plt.tight_layout()
    plt.savefig(output)
    plt.close()


def format_value(value: float) -> str:
    if value >= 1000:
        return f"{value / 1000:.1f}k"
    if value >= 100:
        return f"{value:.0f}"
    if value >= 10:
        return f"{value:.1f}"
    return f"{value:.2f}"


if __name__ == "__main__":
    main()
