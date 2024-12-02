import os
import sys
import shutil
import requests
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import uvicorn
from typing import Optional
from fastapi.responses import FileResponse
from core import run_tts_script, run_download_script

app = FastAPI()

class TTSRequest(BaseModel):
    text: str
    voice: str = "ja-JP-NanamiNeural"
    rate: int = 0
    pitch: int = 3
    filter_radius: int = 4
    index_rate: float = 0.6
    volume_envelope: int = 1
    protect: float = 0.5
    hop_length: int = 256
    f0_method: str = "rmvpe"
    split_audio: bool = False
    f0_autotune: bool = False
    f0_autotune_strength: float = 0.12
    clean_audio: bool = True
    clean_strength: float = 0.5
    export_format: str = "wav"
    upscale_audio: bool = False
    embedder_model: str = "contentvec"
    pth_path: str = "logs/chino-kafuu/chino-kafuu.pth"
    index_path: str = "logs/chino-kafuu/added_IVF209_Flat_nprobe_1_chino-kafuu_v2.index"
    f0_file: str = "https://github.com/gradio-app/gradio/raw/main/test/test_files/sample_file.pdf"
    embedder_model_custom: Optional[str] = None

@app.get("/check_model")
async def check_model():
    model_name = "chino-kafuu"
    logs_dir = os.path.join("logs", model_name)
    pth_path = os.path.join(logs_dir, f"{model_name}.pth")
    index_path = os.path.join(logs_dir, f"added_IVF209_Flat_nprobe_1_{model_name}_v2.index")
    
    if not os.path.exists(pth_path) or not os.path.exists(index_path):
        try:
            model_download_url = "https://huggingface.co/Timur04129/Chino-Kafuu/resolve/main/chino-kafuu.zip"
            os.makedirs(logs_dir, exist_ok=True)
            run_download_script(model_download_url)
            return {"message": "Model tải thành công."}
        except Exception as e:
            raise HTTPException(status_code=500, detail=f"Lỗi tải model: {str(e)}")
    
    return {"message": "Model đã có sẵn."}

@app.get("/download/{file_name}")
async def download_file(file_name: str):
    file_path = os.path.join("downloads", file_name)
    
    if not os.path.exists(file_path):
        raise HTTPException(status_code=404, detail="File không tồn tại.")
    
    return FileResponse(file_path)

@app.post("/tts")
async def text_to_speech(request: TTSRequest):
    model_name = "chino-kafuu"
    logs_dir = os.path.join("logs", model_name)
    pth_path = os.path.join(logs_dir, f"{model_name}.pth")
    index_path = os.path.join(logs_dir, f"added_IVF209_Flat_nprobe_1_{model_name}_v2.index")
    
    if not os.path.exists(pth_path) or not os.path.exists(index_path):
        raise HTTPException(status_code=400, detail="Model chưa tải về.")

    unique_id = os.urandom(4).hex()
    tts_file = os.path.join("tts_temp", f"tts_input_{unique_id}.txt")
    output_tts_path = os.path.join("tts_temp", f"tts_output_{unique_id}.wav")
    output_rvc_path = os.path.join("tts_output", f"rvc_output_{unique_id}.{request.export_format}")
    
    os.makedirs("tts_temp", exist_ok=True)
    os.makedirs("tts_output", exist_ok=True)
    
    with open(tts_file, 'w', encoding='utf-8') as f:
        f.write(request.text)
    
    try:
        result, final_output_path = run_tts_script(
            tts_file=tts_file,
            tts_text=request.text,
            tts_voice=request.voice,
            tts_rate=request.rate,
            pitch=request.pitch,
            filter_radius=request.filter_radius,
            index_rate=request.index_rate,
            volume_envelope=request.volume_envelope,
            protect=request.protect,
            hop_length=request.hop_length,
            f0_method=request.f0_method,
            output_tts_path=output_tts_path,
            output_rvc_path=output_rvc_path,
            pth_path=pth_path,
            index_path=index_path,
            split_audio=request.split_audio,
            f0_autotune=request.f0_autotune,
            f0_autotune_strength=request.f0_autotune_strength,
            clean_audio=request.clean_audio,
            clean_strength=request.clean_strength,
            export_format=request.export_format,
            upscale_audio=request.upscale_audio,
            f0_file="",
            embedder_model=request.embedder_model
        )
        
        os.remove(tts_file)
        os.remove(output_tts_path)
        
        return FileResponse(final_output_path, media_type="audio/wav", headers={"Content-Disposition": "attachment; filename=output.wav"})
    
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Lỗi TTS: {str(e)}")

# Chạy server
if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=8000)
