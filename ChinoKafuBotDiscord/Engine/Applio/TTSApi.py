﻿from gradio_client import Client

client = Client("http://127.0.0.1:6969/")

def TTS(message):
    result = client.predict(
		tts_text=message,	# str  in 'Text to Synthesize' Textbox component
		tts_voice="ja-JP-NanamiNeural",	# Literal['af-ZA-AdriNeural', 'af-ZA-WillemNeural', 'sq-AL-AnilaNeural', 'sq-AL-IlirNeural', 'am-ET-AmehaNeural', 'am-ET-MekdesNeural', 'ar-DZ-AminaNeural', 'ar-DZ-IsmaelNeural', 'ar-BH-AliNeural', 'ar-BH-LailaNeural', 'ar-EG-SalmaNeural', 'ar-EG-ShakirNeural', 'ar-IQ-BasselNeural', 'ar-IQ-RanaNeural', 'ar-JO-SanaNeural', 'ar-JO-TaimNeural', 'ar-KW-FahedNeural', 'ar-KW-NouraNeural', 'ar-LB-LaylaNeural', 'ar-LB-RamiNeural', 'ar-LY-ImanNeural', 'ar-LY-OmarNeural', 'ar-MA-JamalNeural', 'ar-MA-MounaNeural', 'ar-OM-AbdullahNeural', 'ar-OM-AyshaNeural', 'ar-QA-AmalNeural', 'ar-QA-MoazNeural', 'ar-SA-HamedNeural', 'ar-SA-ZariyahNeural', 'ar-SY-AmanyNeural', 'ar-SY-LaithNeural', 'ar-TN-HediNeural', 'ar-TN-ReemNeural', 'ar-AE-FatimaNeural', 'ar-AE-HamdanNeural', 'ar-YE-MaryamNeural', 'ar-YE-SalehNeural', 'az-AZ-BabekNeural', 'az-AZ-BanuNeural', 'bn-BD-NabanitaNeural', 'bn-BD-PradeepNeural', 'bn-IN-BashkarNeural', 'bn-IN-TanishaaNeural', 'bs-BA-GoranNeural', 'bs-BA-VesnaNeural', 'bg-BG-BorislavNeural', 'bg-BG-KalinaNeural', 'my-MM-NilarNeural', 'my-MM-ThihaNeural', 'ca-ES-EnricNeural', 'ca-ES-JoanaNeural', 'zh-HK-HiuGaaiNeural', 'zh-HK-HiuMaanNeural', 'zh-HK-WanLungNeural', 'zh-CN-XiaoxiaoNeural', 'zh-CN-XiaoyiNeural', 'zh-CN-YunjianNeural', 'zh-CN-YunxiNeural', 'zh-CN-YunxiaNeural', 'zh-CN-YunyangNeural', 'zh-CN-liaoning-XiaobeiNeural', 'zh-TW-HsiaoChenNeural', 'zh-TW-YunJheNeural', 'zh-TW-HsiaoYuNeural', 'zh-CN-shaanxi-XiaoniNeural', 'hr-HR-GabrijelaNeural', 'hr-HR-SreckoNeural', 'cs-CZ-AntoninNeural', 'cs-CZ-VlastaNeural', 'da-DK-ChristelNeural', 'da-DK-JeppeNeural', 'nl-BE-ArnaudNeural', 'nl-BE-DenaNeural', 'nl-NL-ColetteNeural', 'nl-NL-FennaNeural', 'nl-NL-MaartenNeural', 'en-AU-NatashaNeural', 'en-AU-WilliamNeural', 'en-CA-ClaraNeural', 'en-CA-LiamNeural', 'en-HK-SamNeural', 'en-HK-YanNeural', 'en-IN-NeerjaExpressiveNeural', 'en-IN-NeerjaNeural', 'en-IN-PrabhatNeural', 'en-IE-ConnorNeural', 'en-IE-EmilyNeural', 'en-KE-AsiliaNeural', 'en-KE-ChilembaNeural', 'en-NZ-MitchellNeural', 'en-NZ-MollyNeural', 'en-NG-AbeoNeural', 'en-NG-EzinneNeural', 'en-PH-JamesNeural', 'en-PH-RosaNeural', 'en-SG-LunaNeural', 'en-SG-WayneNeural', 'en-ZA-LeahNeural', 'en-ZA-LukeNeural', 'en-TZ-ElimuNeural', 'en-TZ-ImaniNeural', 'en-GB-LibbyNeural', 'en-GB-MaisieNeural', 'en-GB-RyanNeural', 'en-GB-SoniaNeural', 'en-GB-ThomasNeural', 'en-US-AriaNeural', 'en-US-AnaNeural', 'en-US-AndrewNeural', 'en-US-AvaNeural', 'en-US-ChristopherNeural', 'en-US-EricNeural', 'en-US-GuyNeural', 'en-US-JennyNeural', 'en-US-MichelleNeural', 'en-US-RogerNeural', 'en-US-SteffanNeural', 'et-EE-AnuNeural', 'et-EE-KertNeural', 'fil-PH-AngeloNeural', 'fil-PH-BlessicaNeural', 'fi-FI-HarriNeural', 'fi-FI-NooraNeural', 'fr-BE-CharlineNeural', 'fr-BE-GerardNeural', 'fr-CA-AntoineNeural', 'fr-CA-JeanNeural', 'fr-CA-SylvieNeural', 'fr-FR-DeniseNeural', 'fr-FR-EloiseNeural', 'fr-FR-HenriNeural', 'fr-CH-ArianeNeural', 'fr-CH-FabriceNeural', 'gl-ES-RoiNeural', 'gl-ES-SabelaNeural', 'ka-GE-EkaNeural', 'ka-GE-GiorgiNeural', 'de-AT-IngridNeural', 'de-AT-JonasNeural', 'de-DE-AmalaNeural', 'de-DE-ConradNeural', 'de-DE-KatjaNeural', 'de-DE-KillianNeural', 'de-CH-JanNeural', 'de-CH-LeniNeural', 'el-GR-AthinaNeural', 'el-GR-NestorasNeural', 'gu-IN-DhwaniNeural', 'gu-IN-NiranjanNeural', 'he-IL-AvriNeural', 'he-IL-HilaNeural', 'hi-IN-MadhurNeural', 'hi-IN-SwaraNeural', 'hu-HU-NoemiNeural', 'hu-HU-TamasNeural', 'is-IS-GudrunNeural', 'is-IS-GunnarNeural', 'id-ID-ArdiNeural', 'id-ID-GadisNeural', 'ga-IE-ColmNeural', 'ga-IE-OrlaNeural', 'it-IT-DiegoNeural', 'it-IT-ElsaNeural', 'it-IT-IsabellaNeural', 'ja-JP-KeitaNeural', 'ja-JP-NanamiNeural', 'jv-ID-DimasNeural', 'jv-ID-SitiNeural', 'kn-IN-GaganNeural', 'kn-IN-SapnaNeural', 'kk-KZ-AigulNeural', 'kk-KZ-DauletNeural', 'km-KH-PisethNeural', 'km-KH-SreymomNeural', 'ko-KR-InJoonNeural', 'ko-KR-SunHiNeural', 'lo-LA-ChanthavongNeural', 'lo-LA-KeomanyNeural', 'lv-LV-EveritaNeural', 'lv-LV-NilsNeural', 'lt-LT-LeonasNeural', 'lt-LT-OnaNeural', 'mk-MK-AleksandarNeural', 'mk-MK-MarijaNeural', 'ms-MY-OsmanNeural', 'ms-MY-YasminNeural', 'ml-IN-MidhunNeural', 'ml-IN-SobhanaNeural', 'mt-MT-GraceNeural', 'mt-MT-JosephNeural', 'mr-IN-AarohiNeural', 'mr-IN-ManoharNeural', 'mn-MN-BataaNeural', 'mn-MN-YesuiNeural', 'ne-NP-HemkalaNeural', 'ne-NP-SagarNeural', 'nb-NO-FinnNeural', 'nb-NO-PernilleNeural', 'ps-AF-GulNawazNeural', 'ps-AF-LatifaNeural', 'fa-IR-DilaraNeural', 'fa-IR-FaridNeural', 'pl-PL-MarekNeural', 'pl-PL-ZofiaNeural', 'pt-BR-AntonioNeural', 'pt-BR-FranciscaNeural', 'pt-PT-DuarteNeural', 'pt-PT-RaquelNeural', 'ro-RO-AlinaNeural', 'ro-RO-EmilNeural', 'ru-RU-DmitryNeural', 'ru-RU-SvetlanaNeural', 'sr-RS-NicholasNeural', 'sr-RS-SophieNeural', 'si-LK-SameeraNeural', 'si-LK-ThiliniNeural', 'sk-SK-LukasNeural', 'sk-SK-ViktoriaNeural', 'sl-SI-PetraNeural', 'sl-SI-RokNeural', 'so-SO-MuuseNeural', 'so-SO-UbaxNeural', 'es-AR-ElenaNeural', 'es-AR-TomasNeural', 'es-BO-MarceloNeural', 'es-BO-SofiaNeural', 'es-CL-CatalinaNeural', 'es-CL-LorenzoNeural', 'es-CO-GonzaloNeural', 'es-CO-SalomeNeural', 'es-CR-JuanNeural', 'es-CR-MariaNeural', 'es-CU-BelkysNeural', 'es-CU-ManuelNeural', 'es-DO-EmilioNeural', 'es-DO-RamonaNeural', 'es-EC-AndreaNeural', 'es-EC-LuisNeural', 'es-SV-LorenaNeural', 'es-SV-RodrigoNeural', 'es-GQ-JavierNeural', 'es-GQ-TeresaNeural', 'es-GT-AndresNeural', 'es-GT-MartaNeural', 'es-HN-CarlosNeural', 'es-HN-KarlaNeural', 'es-MX-DaliaNeural', 'es-MX-JorgeNeural', 'es-NI-FedericoNeural', 'es-NI-YolandaNeural', 'es-PA-MargaritaNeural', 'es-PA-RobertoNeural', 'es-PY-MarioNeural', 'es-PY-TaniaNeural', 'es-PE-AlexNeural', 'es-PE-CamilaNeural', 'es-PR-KarinaNeural', 'es-PR-VictorNeural', 'es-ES-AlvaroNeural', 'es-ES-ElviraNeural', 'es-US-AlonsoNeural', 'es-US-PalomaNeural', 'es-UY-MateoNeural', 'es-UY-ValentinaNeural', 'es-VE-PaolaNeural', 'es-VE-SebastianNeural', 'su-ID-JajangNeural', 'su-ID-TutiNeural', 'sw-KE-RafikiNeural', 'sw-KE-ZuriNeural', 'sw-TZ-DaudiNeural', 'sw-TZ-RehemaNeural', 'sv-SE-MattiasNeural', 'sv-SE-SofieNeural', 'ta-IN-PallaviNeural', 'ta-IN-ValluvarNeural', 'ta-MY-KaniNeural', 'ta-MY-SuryaNeural', 'ta-SG-AnbuNeural', 'ta-SG-VenbaNeural', 'ta-LK-KumarNeural', 'ta-LK-SaranyaNeural', 'te-IN-MohanNeural', 'te-IN-ShrutiNeural', 'th-TH-NiwatNeural', 'th-TH-PremwadeeNeural', 'tr-TR-AhmetNeural', 'tr-TR-EmelNeural', 'uk-UA-OstapNeural', 'uk-UA-PolinaNeural', 'ur-IN-GulNeural', 'ur-IN-SalmanNeural', 'ur-PK-AsadNeural', 'ur-PK-UzmaNeural', 'uz-UZ-MadinaNeural', 'uz-UZ-SardorNeural', 'vi-VN-HoaiMyNeural', 'vi-VN-NamMinhNeural', 'cy-GB-AledNeural', 'cy-GB-NiaNeural', 'zu-ZA-ThandoNeural', 'zu-ZA-ThembaNeural']  in 'Tiếng nói TTS' Dropdown component
		tts_rate=5,	# float (numeric value between -24 and 24) in 'Pitch' Slider component
		f0up_key=4,	# float (numeric value between 0 and 7) in 'Filter Radius' Slider component
		filter_radius=0.8,	# float (numeric value between 0 and 1) in 'Search Feature Ratio' Slider component
		index_rate=1,	# float (numeric value between 0 and 1) in 'Volume Envelope' Slider component
		rms_mix_rate=0.5,	# float (numeric value between 0 and 0.5) in 'Protect Voiceless Consonants' Slider component
		hop_length=128,	# float (numeric value between 1 and 512) in 'Hop Length' Slider component
		f0method="rmvpe",	# Literal['pm', 'harvest', 'dio', 'crepe', 'crepe-tiny', 'rmvpe', 'fcpe', 'hybrid[rmvpe+fcpe]']  in 'Pitch extraction algorithm' Radio component
		output_tts_path="result",	# str  in 'Output Path for TTS Audio' Textbox component
		output_rvc_path="result.wav",	# str  in 'Output Path for RVC Audio' Textbox component
		pth_path="logs\chino-kafuu\chino-kafuu.pth",	# Literal['logs\chino-kafuu\chino-kafuu.pth', 'logs\Chino-KafA-Singing-RMVPE\Chino-KafA-Singing-RMVPE.pth', 'logs\TendouAlice\TendouAlice.pth']  in 'Mô hình giọng nói' Dropdown component
		index_path="logs\chino-kafuu\added_IVF209_Flat_nprobe_1_chinno-kafuu_v2.index",	# Literal['logs\Chino-KafA-Singing-RMVPE\added_IVF772_Flat_nprobe_1_Chino-KafA-Singing-RMVPE_v2.index', 'logs\chino-kafuu\added_IVF209_Flat_nprobe_1_chino-kafuu_v2.index', 'logs\TendouAlice\added_IVF933_Flat_nprobe_1_TendouAlice_v2.index']  in 'Tệp chỉ mục' Dropdown component
		split_audio=False,	# bool  in 'Split Audio' Checkbox component
		f0autotune=False,	# bool  in 'Autotune' Checkbox component
		clean_audio=True,	# bool  in 'Clean Audio' Checkbox component
		clean_strength=0,	# float (numeric value between 0 and 1) in 'Clean Strength' Slider component
		export_format="WAV",	# Literal['WAV', 'MP3', 'FLAC', 'OGG', 'M4A']  in 'Export Format' Radio component
        upscale_audio=False,
        embedder_model="hubert",
		api_name="/run_tts_script"
)
    