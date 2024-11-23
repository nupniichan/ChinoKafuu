import os
from core import run_tts_script 

def TTS(message, guild_id, message_id):
    output_folder = os.path.join("..", "ChinoKafuu", "CommunicationHistory", "VoiceHistory", str(guild_id))
    output_file = f"result_{guild_id}_{message_id}.wav"
    
    os.makedirs(output_folder, exist_ok=True)
    full_output_path = os.path.join(output_folder, output_file)
    
    tts_file = None  
    tts_text = message
    tts_voice = "ja-JP-NanamiNeural"
    tts_rate = 0
    pitch = 3
    filter_radius = 4
    index_rate = 0.6
    volume_envelope = 1
    protect = 0.5
    hop_length = 256
    f0_method = "rmvpe"
    output_tts_path = "./temporary/temporary_tts_output.wav"
    output_rvc_path = full_output_path
    pth_path = "logs/chino-kafuu/chino-kafuu.pth"
    index_path = "logs/chino-kafuu/added_IVF209_Flat_nprobe_1_chino-kafuu_v2.index"
    split_audio = False
    f0_autotune = True
    clean_audio = True
    clean_strength = 0.5
    export_format = "WAV"
    upscale_audio = False
    f0_file = None
    embedder_model = "contentvec"
    embedder_model_custom = None
    sid = 0

    result, output_path = run_tts_script(
        tts_file=tts_file,
        tts_text=tts_text,
        tts_voice=tts_voice,
        tts_rate=tts_rate,
        pitch=pitch,
        filter_radius=filter_radius,
        index_rate=index_rate,
        volume_envelope=volume_envelope,
        protect=protect,
        hop_length=hop_length,
        f0_method=f0_method,
        output_tts_path=output_tts_path,
        output_rvc_path=output_rvc_path,
        pth_path=pth_path,
        index_path=index_path,
        split_audio=split_audio,
        f0_autotune=f0_autotune,
        f0_autotune_strength=1.0,
        clean_audio=clean_audio,
        clean_strength=clean_strength,
        export_format=export_format,
        upscale_audio=upscale_audio,
        f0_file=f0_file,
        embedder_model=embedder_model,
        embedder_model_custom=embedder_model_custom,
        sid=sid,
    )
    return result, output_path


# Test 
if __name__ == "__main__":
    message = "こんにちは、これはテストです！"  
    result, output_path = TTS(message)
    print("TTS Result:", result)
    print("Output File Path:", output_path)
