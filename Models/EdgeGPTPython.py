import asyncio, sys
from EdgeGPT.EdgeUtils import Query, Cookie

style_map = {
    "creative": ConversationStyle.creative,
    "precise": ConversationStyle.precise,
    "balanced": ConversationStyle.balanced
}
style_arg = sys.argv[1]
message = sys.argv[2]
style = style_map.get(style_arg, ConversationStyle.creative)

# Use a for loop with range(max_tries)
for _ in range(3):
    try:
        q = Query(message, style=style, ignore_cookies=True)
        print(q)
        break # Exit the loop if no error
    except Exception as e:
        print(f"Error: {e}\n\n")
        if _ == 2: # Check if max tries reached
            print("Maximum number of tries reached. Exiting.")
