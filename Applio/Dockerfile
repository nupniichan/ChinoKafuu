FROM python:3.9.0

WORKDIR /app
COPY . .
RUN ls -l  
RUN pip install --upgrade pip
RUN pip install -r requirements.txt
EXPOSE 5000
CMD ["python", "tts_api.py"]