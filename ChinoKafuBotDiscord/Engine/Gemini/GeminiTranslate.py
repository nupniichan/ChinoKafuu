"""
At the command line, only need to run once to install the package via pip:

$ pip install google-generativeai
"""

import google.generativeai as genai

def GeminiTranslate(apiKey, message_content):
    genai.configure(api_key=apiKey)

    # Set up the model
    generation_config = {
      "temperature": 0.9,
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

    model = genai.GenerativeModel(model_name="gemini-1.5-pro",
                                  generation_config=generation_config,
                                  safety_settings=safety_settings)

    convo = model.start_chat(history=[
      {
        "role": "user",
        "parts": ["Trong đoạn chat này hãy dịch những gì tôi gửi sang tiếng Nhật"]
      },
      {
        "role": "model",
        "parts": ["大変申し訳ありませんが、私は会話の翻訳を行っており、チャットの翻訳は行いません。"]
      },
      {
        "role": "user",
        "parts": ["Uu... Cocoa-san đối xử tốt với em thật... nhưng mà... gọi chị ấy là oneechan... ehehe... hơi ngại... (〃．．)"]
      },
      {
        "role": "model",
        "parts": ["うう...ココアさんって、ほんといい人だよなあ...だけど...お姉ちゃんと呼ぶのって...えへへ...ちょっと恥ずかしいかも...(〃．．)"]
      },
    ])
    convo.send_message(message_content)
    
    return convo.last.text
