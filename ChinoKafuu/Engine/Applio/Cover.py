from gradio_client import Client

client = Client("http://127.0.0.1:6969/")

def TTS(message):
	result = client.predict(
		-24,	# float (numeric value between -24 and 24) in 'Pitch' Slider component
		0,	# float (numeric value between 0 and 7) in 'Filter Radius' Slider component
		0,	# float (numeric value between 0 and 1) in 'Search Feature Ratio' Slider component
		0,	# float (numeric value between 0 and 1) in 'Volume Envelope' Slider component
		0,	# float (numeric value between 0 and 0.5) in 'Protect Voiceless Consonants' Slider component
		1,	# float (numeric value between 1 and 512) in 'Hop Length' Slider component
		"rmvpe",	# Literal['pm', 'harvest', 'dio', 'crepe', 'crepe-tiny', 'rmvpe', 'fcpe', 'hybrid[rmvpe+fcpe]']  in 'Pitch extraction algorithm' Radio component
		null,	# Literal[]  in 'Select Audio' Dropdown component
		"Cover",	# str  in 'Output Path' Textbox component
		"logs\chino-kafuu\chino-kafuu.pth",	# Literal['logs\chino-kafuu\chino-kafuu.pth']  in 'Voice Model' Dropdown component
		"logs\chino-kafuu\added_IVF209_Flat_nprobe_1_chino-kafuu_v2.index",	# Literal['logs\chino-kafuu\added_IVF209_Flat_nprobe_1_chino-kafuu_v2.index']  in 'Index File' Dropdown component
		True,	# bool  in 'Split Audio' Checkbox component
		True,	# bool  in 'Autotune' Checkbox component
		True,	# bool  in 'Clean Audio' Checkbox component
		0,	# float (numeric value between 0 and 1) in 'Clean Strength' Slider component
		"WAV",	# Literal['WAV', 'MP3', 'FLAC', 'OGG', 'M4A']  in 'Export Format' Radio component
		api_name="/run_infer_script"
)