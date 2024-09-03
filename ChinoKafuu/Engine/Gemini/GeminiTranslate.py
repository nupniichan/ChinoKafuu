import os
import google.generativeai as genai

def GeminiTranslate(apiKey, message_content):

    genai.configure(api_key=apiKey)

    generation_config = {
      "temperature": 0.9,
      "top_p": 1,
      "top_k": 1,
      "max_output_tokens": 1024,
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

    convo = model.start_chat(history=[
{
      "role": "user",
      "parts": [
        "Trong đoạn chat này hãy dịch những gì tôi gửi sang tiếng Nhật. Đồng thời đừng dịch các đoạn trong dấu * và /n và \n ví dụ: *cười nhẹ* thì xoá nó luôn cũng như là các emoji ví dụ như: (^▽^), (≧∇≦), (^▽^) ,v.v và các emoji của discord được sử dụng trong cặp dấu :. Chỉ cần giữ lại đoạn trò chuyện chính thôi",
      ],
    },
    {
      "role": "model",
      "parts": [
        "こんにちは  お元気ですか？",
      ],
    },
    ])

    try:
        convo.send_message(message_content)
        return convo.last.text
    except genai.exception.ApiException as e:
        print(f"Lỗi khi gọi API Google Gemini: {e}")
        return "Xin lỗi, hiện tại em không thể dịch được. Anh thử lại sau nhé~"