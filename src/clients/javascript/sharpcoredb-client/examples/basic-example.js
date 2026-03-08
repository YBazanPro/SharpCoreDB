#!/usr/bin/env node

/**
 * Basic Connection Example
 * Demonstrates fundamental database operations with @sharpcoredb/client
 */

const { connect } = require('../dist/index.js');

async function main() {
  console.log('🚀 @sharpcoredb/client Basic Example');
  console.log('=====================================');

  let connection;

  try {
    // Connect to SharpCoreDB Server
    console.log('📡 Connecting to database...');
    connection = await connect('grpc://localhost:5001', {
      database: 'example'
    });
    console.log('✅ Connected successfully');

    // Create a test table
    console.log('📝 Creating test table...');
    await connection.executeNonQuery(`
      CREATE TABLE IF NOT EXISTS users (
        id INTEGER PRIMARY KEY,
        name TEXT NOT NULL,
        age INTEGER,
        email TEXT
      )
    `);
    console.log('✅ Table created');

    // Insert some data
    console.log('📥 Inserting test data...');
    const affected = await connection.executeNonQuery(
      'INSERT INTO users (name, age, email) VALUES (?, ?, ?)',
      { name: 'Alice', age: 30, email: 'alice@example.com' }
    );
    console.log(`✅ Inserted ${affected} row(s)`);

    // Query the data
    console.log('📤 Querying data...');
    const result = await connection.execute('SELECT * FROM users WHERE age >= ?', { age: 25 });
    console.log(`✅ Found ${result.rows.length} user(s) aged 25+:`);

    result.rows.forEach((row, index) => {
      console.log(`  ${index + 1}. ${row.name} (${row.age} years old)`);
    });

    // Ping the server
    console.log('🏓 Pinging server...');
    const latency = await connection.ping();
    console.log(`✅ Server latency: ${latency.toFixed(2)}ms`);

  } catch (error) {
    console.error('❌ Error:', error.message);
    process.exit(1);
  } finally {
    if (connection) {
      console.log('🔌 Closing connection...');
      await connection.close();
      console.log('✅ Connection closed');
    }
  }

  console.log('\n🎉 Example completed successfully!');
}

// Run the example
main().catch(console.error);
