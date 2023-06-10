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
    style = style_map.get(style_arg, ConversationStyle.balanced)
    bot = await Chatbot.create()
    response = await bot.ask(prompt=message, conversation_style=style, simplify_response=True)
    print(json.dumps(response))
    await bot.close()

if __name__ == "__main__":
    asyncio.run(main())