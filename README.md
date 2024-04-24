# Vyssuals WebSocket Server
This is the WebSocket server used to manage communication between the Vyssuals Connectors and the web app. It is a pretty basic WebSocket Server with only one real specification: It forwards every message it receives to all connected clients except the sender.
The idea is that all Vyssuals Connectors should connect to a single instance of this server running on the local machine.
