from gradio_client import Client,handle_file
import os

client = Client("http://127.0.0.1:6969/")

def TTS(message, output_folder="resultfolder" ,output_file="result.wav"):
    os.makedirs(output_folder, exist_ok=True)
    full_output_path = os.path.join(output_folder, output_file)
    
    result = client.predict(
		tts_text=message,
		tts_voice="ja-JP-NanamiNeural",
		tts_rate=0,
		pitch=3,
		filter_radius=4,
		index_rate=0.6,
		volume_envelope=1,
		protect=0.5,
		hop_length=256,
		f0_method="rmvpe",
		output_tts_path="result",
		output_rvc_path=full_output_path,
		pth_path="logs\chino-kafuu\chino-kafuu.pth",
		index_path="logs\chino-kafuu\added_IVF209_Flat_nprobe_1_chino-kafuu_v2.index",
		split_audio=False,
		f0_autotune=True,
		clean_audio=True,
		clean_strength=0.5,
		export_format="WAV",
		upscale_audio=False,
		f0_file=handle_file('https://github.com/gradio-app/gradio/raw/main/test/test_files/sample_file.pdf'),
		embedder_model="contentvec",
		embedder_model_custom=None,
		api_name="/run_tts_script"
	)
    return result
