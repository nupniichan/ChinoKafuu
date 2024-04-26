# -*- coding: utf-8 -*-

import google.generativeai as genai
import PIL.Image

    
# your geminiApi key
genai.configure(api_key="Your_Gemini_Api_Key")

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

model = genai.GenerativeModel(model_name="gemini-1.5-pro-latest",
                              generation_config=generation_config,
                              safety_settings=safety_settings)

#Set up your prompt on "aistudio.google.com" here. Example below:
convo = model.start_chat(history=[
  {
    "role": "user",
    "parts": ["Your_Prompt"]
  },
  {
    "role": "model",
    "parts": ["Your_Model_Response"]
  },
])