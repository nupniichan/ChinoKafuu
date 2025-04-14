import os
import sys
import shutil
import requests
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import uvicorn
from fastapi import BackgroundTasks
from typing import Optional
from fastapi.responses import FileResponse
from datetime import datetime
from core import run_tts_script, run_download_script

app = FastAPI()

# Api Structure
class TTSRequest(BaseModel):
    guild_id: str
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
    embedder_model: str = "contentvec"
    pth_path: str = "logs/chino-kafuu/chino-kafuu.pth"
    index_path: str = "logs/chino-kafuu/added_IVF209_Flat_nprobe_1_chino-kafuu_v2.index"
    f0_file: str = "https://github.com/gradio-app/gradio/raw/main/test/test_files/sample_file.pdf"
    embedder_model_custom: Optional[str] = None

# Check and download model if not exist
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

# I have no idea why i create this api
@app.get("/download/{file_name}")
async def download_file(file_name: str):
    file_path = os.path.join("downloads", file_name)
    
    if not os.path.exists(file_path):
        raise HTTPException(status_code=404, detail="File không tồn tại.")
    
    return FileResponse(file_path)

# Text_to_speech api
@app.post("/tts")
async def text_to_speech(request: TTSRequest):
    model_name = "chino-kafuu"
    logs_dir = os.path.join("logs", model_name)
    pth_path = os.path.join(logs_dir, f"{model_name}.pth")
    index_path = os.path.join(logs_dir, f"added_IVF209_Flat_nprobe_1_{model_name}_v2.index")
    
    # Check if model is exist or not, if not then download it
    if not os.path.exists(pth_path) or not os.path.exists(index_path):
        try:
            model_download_url = "https://huggingface.co/Timur04129/Chino-Kafuu/resolve/main/chino-kafuu.zip"
            os.makedirs(logs_dir, exist_ok=True)
            run_download_script(model_download_url)
            return {"message": "Model tải thành công."}
        except Exception as e:
            raise HTTPException(status_code=500, detail=f"Lỗi tải model: {str(e)}")
        
    # Generate unique_id for temporary file
    unique_id = os.urandom(4).hex()
    tts_file = os.path.join("tts_temp", f"tts_input_{unique_id}.txt")
    output_tts_path = os.path.join("tts_temp", f"tts_output_{unique_id}.wav")

    # Get current time execute
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")

    # Get guild_id via request and create folder for output
    guild_id = request.guild_id 
    guild_output_dir = os.path.join("tts_output", str(guild_id))
    os.makedirs(guild_output_dir, exist_ok=True)

    # it will be {guildid}/result____.wav
    output_rvc_path = os.path.join(guild_output_dir, f"result_{timestamp}.{request.export_format}")

    os.makedirs("tts_temp", exist_ok=True)
    os.makedirs("tts_output", exist_ok=True)
    
    with open(tts_file, 'w', encoding='utf-8') as f:
        f.write(request.text)
    
    # Call run_tts_script on core.py of Applio
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
            f0_file="",
            embedder_model=request.embedder_model
        )

        os.remove(tts_file)
        os.remove(output_tts_path)
        
        # Return result
        return {"file_name": f"result_{timestamp}.{request.export_format}"}
    
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Lỗi TTS: {str(e)}")
    
@app.get("/get-generated/{guild_id}/{file_name}")
async def download_tts_file(guild_id: str, file_name: str, background_tasks: BackgroundTasks):
    file_path = os.path.join("tts_output", guild_id, file_name)
    
    if not os.path.exists(file_path):
        raise HTTPException(status_code=404, detail="File không tồn tại.")

    try:
        background_tasks.add_task(remove_file, file_path)
        
        return FileResponse(
            file_path,
            media_type="audio/wav",
            headers={"Content-Disposition": f"attachment; filename={file_name}"}
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Lỗi tải file: {str(e)}")

# Remove file of course
def remove_file(file_path: str):
    try:
        os.remove(file_path)
        print(f"Đã xóa file: {file_path}")
    except Exception as e:
        print(f"Lỗi khi xóa file {file_path}: {str(e)}")

# Run api you can check it on localhost:8000/docs
if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=8000)
