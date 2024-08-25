import asyncio
import websockets
import random


async def WebSocketHandler(websocket):
    while True:
        try:
            # Get data from message
            data = await websocket.recv()
            print(f'Server received: {data}')

            greeting = f"{data}"

            # Waiting
            await asyncio.sleep(3)

            # Send new message
            await websocket.send(greeting)
        except websockets.exceptions.ConnectionClosed:
            print("Client disconnected")
            break

        await asyncio.sleep(random.random() * 3)


# Main function
async def main():
    async with websockets.serve(WebSocketHandler, "localhost", 8080) as websocket:
        print(f"WebSocket server started on {8080} port")
        await asyncio.Future()


if __name__ == "__main__":
    asyncio.run(main())
