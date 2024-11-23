from flask import Flask, request, send_file
from main_tts import TTS
import os

app = Flask(__name__)

@app.route('/tts', methods=['POST'])
def generate_tts():
    try:
        data = request.json
        message = data.get('message')
        guild_id = data.get('guildId')
        message_id = os.urandom(4).hex()
        
        # Gọi hàm TTS với các tham số cần thiết
        result, output_path = TTS(message, guild_id, message_id)
        
        # Trả về đường dẫn file
        return {
            'success': True,
            'file_path': output_path
        }
    except Exception as e:
        return {
            'success': False,
            'error': str(e)
        }, 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)