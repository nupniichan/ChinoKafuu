import google.generativeai as genai
import json
import os

MAX_CHAT_HISTORY_LENGTH = 50

def RunGeminiAPI(geminiAPIKey, message_content, username, chat_history_path=os.path.abspath("../../../Engine/Gemini/HistoryChat/chat_history.json")):
    genai.configure(api_key=geminiAPIKey)

    generation_config = {
        "temperature": 1.2,
        "top_p": 1,
        "top_k": 1,
        "max_output_tokens": 512,
    }

    safety_settings = [
        {
        "category": "HARM_CATEGORY_HARASSMENT",
        "threshold": "BLOCK_NONE"
        },
        {
        "category": "HARM_CATEGORY_HATE_SPEECH",
        "threshold": "BLOCK_NONE"
        },
        {
        "category": "HARM_CATEGORY_SEXUALLY_EXPLICIT",
        "threshold": "BLOCK_NONE"
        },
        {
        "category": "HARM_CATEGORY_DANGEROUS_CONTENT",
        "threshold": "BLOCK_NONE"
        },
    ]

    model = genai.GenerativeModel(model_name="gemini-1.5-flash",
                    generation_config=generation_config,
                    safety_settings=safety_settings)

    os.makedirs(os.path.dirname(chat_history_path), exist_ok=True)
    
    try:
        with open(chat_history_path, "r", encoding='utf-8') as f:
            chat_history = json.load(f)
    except FileNotFoundError:
        chat_history = []

    chat_history = chat_history[-MAX_CHAT_HISTORY_LENGTH:]

    with open("../../../Engine/Gemini/Prompt/prompt.txt", "r", encoding="utf-8") as file:
        prompt_text = file.read()
        
    initial_prompt = [
        {
        "role": "user",
        "parts": [prompt_text]
        },
        {
        "role": "model",
        "parts": ["Ah, chào mừng anh đến với Rabbit House (≧◡≦)."]
        },
    ]
    
    initial_prompt = initial_prompt + chat_history 
    chat_session = model.start_chat(history=initial_prompt)
    
    try:
        chat_session.send_message(username + ": " + message_content)

        chat_history.append({
            "role": "user",
            "parts": [username + ": " + message_content]
        })
        chat_history.append({
            "role": "model",
            "parts": [chat_session.last.text]
        })

        with open(chat_history_path, "w", encoding='utf-8') as f:
            json.dump(chat_history, f, indent=4, ensure_ascii=False)

        return chat_session.last.text
    
    except genai.exception.ApiException as e:
        print(f"Lỗi khi gọi API Google Gemini: {e}")
        return "Xin lỗi, hiện tại em không thể trả lời được. Anh thử lại sau nhé~" 