import { ApolloServer } from '@apollo/server';
import { startStandaloneServer } from '@apollo/server/standalone';
import { pool } from './db.js';
import { resolvers } from './resolvers.js';
import { typeDefs } from './schema.js';

const port = Number(process.env.PORT ?? 5002);

const server = new ApolloServer({
  typeDefs,
  resolvers
});

const { url } = await startStandaloneServer(server, {
  listen: { host: '0.0.0.0', port }
});

console.log(`GraphQL service ready at ${url}`);

const shutdown = async () => {
  await server.stop();
  await pool.end();
  process.exit(0);
};

process.on('SIGINT', shutdown);
process.on('SIGTERM', shutdown);
