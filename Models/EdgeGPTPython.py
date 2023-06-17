import asyncio, json, sys
from EdgeGPT.EdgeGPT import Chatbot, ConversationStyle

async def main():
    style_map = {
        "creative": ConversationStyle.creative,
        "precise": ConversationStyle.precise,
        "balanced": ConversationStyle.balanced
    }
    style_arg = sys.argv[1]
    message = sys.argv[2]
    style = style_map.get(style_arg, ConversationStyle.creative)
    bot = await Chatbot.create()
    # Add a counter and a loop to retry the ask method
    counter = 0
    max_tries = 3
    while counter < max_tries:
        try:
            response = await bot.ask(prompt=message, conversation_style=style, simplify_response=True)
            print(json.dumps(response))
            break # Exit the loop if no error
        except Exception as e:
            print(f"Error: {e}")
            counter += 1 # Increment the counter
            if counter == max_tries:
                print("Maximum number of tries reached. Exiting.")
                break # Exit the loop if max tries reached
    await bot.close()

if __name__ == "__main__":
    asyncio.run(main())
