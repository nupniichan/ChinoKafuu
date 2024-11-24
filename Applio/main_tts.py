import os
from core import run_tts_script

def TTS(message, guild_id, message_id, output_folder):
    # Định dạng tên file
    output_file = f"result_{guild_id}_{message_id}.wav"
    full_output_path = os.path.join(output_folder, output_file)
    
    # Tạo temporary file cho TTS
    temp_tts_file = os.path.join(output_folder, f"temp_tts_{guild_id}_{message_id}.wav")

    # Gọi hàm xử lý TTS của Applio
    result, output_path = run_tts_script(
        tts_file="",
        tts_text=message,
        output_tts_path=temp_tts_file,
        output_rvc_path=full_output_path,
        tts_voice="ja-JP-NanamiNeural",
        tts_rate=0,
        pitch=3,
        filter_radius=4,
        index_rate=0.6,
        volume_envelope=1,
        protect=0.5,
        hop_length=256,
        f0_method="rmvpe",
        pth_path="logs/chino-kafuu/chino-kafuu.pth",
        index_path="logs/chino-kafuu/added_IVF209_Flat_nprobe_1_chino-kafuu_v2.index",
        split_audio=False,
        f0_autotune=False,
        f0_autotune_strength=0.0,
        clean_audio=True,
        clean_strength=0.7,
        export_format="WAV",
        upscale_audio=False,
        f0_file=None,
        embedder_model="contentvec",
        embedder_model_custom=None,
        sid=0
    )

    # Xóa file TTS tạm thời nếu tồn tại
    if os.path.exists(temp_tts_file):
        os.remove(temp_tts_file)

    return result, output_path
