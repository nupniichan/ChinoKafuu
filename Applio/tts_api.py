import os
from flask import Flask, request, send_file
from main_tts import TTS

app = Flask(__name__)

# Thư mục cơ sở để lưu file
BASE_OUTPUT_FOLDER = os.path.join("CommunicationHistory", "HistoryChat")

@app.route('/tts', methods=['POST'])
def generate_tts():
    try:
        # Lấy dữ liệu từ request
        data = request.json
        message = data.get('message')
        guild_id = str(data.get('guildId'))  # Chuyển guildId thành chuỗi
        message_id = os.urandom(4).hex()

        # Tạo thư mục lưu file theo guildId
        guild_folder = os.path.join(BASE_OUTPUT_FOLDER, guild_id)
        os.makedirs(guild_folder, exist_ok=True)

        # Gọi hàm TTS để tạo file
        result, output_path = TTS(message, guild_id, message_id, output_folder=guild_folder)

        # Trả về đường dẫn file tương đối cho client
        return {
            'success': True,
            'file_path': os.path.relpath(output_path, BASE_OUTPUT_FOLDER)
        }
    except Exception as e:
        return {
            'success': False,
            'error': str(e)
        }, 500


@app.route('/download', methods=['GET'])
def download_file():
    file_path = request.args.get('file')
    full_file_path = os.path.join(BASE_OUTPUT_FOLDER, file_path)
    if os.path.exists(full_file_path):
        return send_file(full_file_path, as_attachment=True)
    else:
        return {"success": False, "error": "File not found"}, 404


if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)
