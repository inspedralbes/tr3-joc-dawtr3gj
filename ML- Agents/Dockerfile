FROM python:3.10.12-slim
ENV PIP_NO_CACHE_DIR=1
ENV PYTHONDONTWRITEBYTECODE=1
ENV PYTHONUNBUFFERED=1
RUN pip install --upgrade pip
RUN pip install "setuptools<81" wheel
# Versions conservadores per evitar el nou exportador ONNX de PyTorch
RUN pip install "torch==2.2.2" "onnx==1.15.0"
# RUN pip install onnx onnxscript
RUN pip install mlagents==1.1.0
WORKDIR /workspace