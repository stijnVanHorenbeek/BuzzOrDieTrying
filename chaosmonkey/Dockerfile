FROM python:3.11-slim
WORKDIR /app
COPY chaosmonkey.py .
RUN pip install requests flask
CMD ["python", "chaosmonkey.py"]
