import asyncio
import websockets
import random
import torch
from transformers import pipeline, AutoTokenizer, AutoModelForCausalLM

# Название модели
model_name = "ai-forever/rugpt3small_based_on_gpt2"

# Определяем устройство, на котором будет запущена LLM (CPU или GPU)
device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
pipe = pipeline("text-generation", model=model_name, device=device)

# Определение токенайзера и модели
tokenizer = AutoTokenizer.from_pretrained(model_name)
model = AutoModelForCausalLM.from_pretrained(model_name)

# Установка модели определённого режима выполнения
if device == 'cuda':
    model.cuda()

async def WebSocketHandler(websocket):
    print("Client connected: ", websocket)
    while True:
        try:
            # Get data from message
            data = await websocket.recv()

            prompt = f"{data}"

            input_ids = tokenizer.encode(prompt, return_tensors="pt")

            if device == 'cuda':
                input_ids = input_ids.cuda()

            # Получение результата от модели
            out = model.generate(input_ids, max_length=64, repetition_penalty=5.0, do_sample=True, top_k=5,
                                 top_p=0.95, temperature=1)

            # Выбираем самый первый текст
            generated_text = list(map(tokenizer.decode, out))[0]

            # Формирование результата
            result = str(generated_text).removeprefix(prompt)

            # Waiting
            # await asyncio.sleep(3)

            # Send new message
            await websocket.send(result)
        except websockets.exceptions.ConnectionClosed:
            print("Client disconnected: ", websocket)
            break

        await asyncio.sleep(random.random() * 3)


# Main function
async def main():
    async with websockets.serve(WebSocketHandler, "localhost", 8080) as websocket:
        print(f"WebSocket server started on {8080} port")
        await asyncio.Future()


if __name__ == "__main__":
    asyncio.run(main())
