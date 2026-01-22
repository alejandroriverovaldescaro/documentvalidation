// Frontend logic for webcam capture, file upload, and API interaction

const webcam = document.getElementById('webcam');
const captureBtn = document.getElementById('capture');
const selfieCanvas = document.getElementById('selfieCanvas');
const livenessStatus = document.getElementById('livenessStatus');
const idImageInput = document.getElementById('idImage');
const verifyBtn = document.getElementById('verifyBtn');
const idPreview = document.getElementById('idPreview');
const resultDiv = document.getElementById('result');

let selfieBlob = null;
let idBlob = null;

// Webcam setup
if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
  navigator.mediaDevices.getUserMedia({ video: true })
    .then((stream) => { webcam.srcObject = stream; });
}

captureBtn.onclick = () => {
  selfieCanvas.getContext('2d').drawImage(webcam, 0, 0, 320, 240);
  selfieCanvas.toBlob(async (blob) => {
    selfieBlob = blob;
    livenessStatus.textContent = 'Detecting liveness...';
    const fd = new FormData();
    fd.append('selfie', blob);
    // Liveness API call
    const resp = await fetch('/api/liveness/detect', {
      method: 'POST',
      body: fd
    });
    const data = await resp.json();
    if (data.liveness?.isLive) {
      livenessStatus.textContent = `Liveness detected! (Score: ${data.liveness.score})`;
    } else {
      livenessStatus.textContent = "Liveness not detected.";
    }
  });
  selfieCanvas.style.display = 'block';
};

idImageInput.onchange = () => {
  const f = idImageInput.files[0];
  if (f) {
    idBlob = f;
    idPreview.src = URL.createObjectURL(f);
    idPreview.style.display = 'block';
  }
};

verifyBtn.onclick = async () => {
  if (!selfieBlob || !idBlob) {
    resultDiv.textContent = 'Please capture selfie and upload ID document.';
    return;
  }
  const fd = new FormData();
  fd.append('selfie', selfieBlob);
  fd.append('id_image', idBlob);
  resultDiv.textContent = 'Verifying...';
  const resp = await fetch('/api/liveness/verify', { method: 'POST', body: fd });
  const data = await resp.json();
  if (data.error) {
    resultDiv.textContent = `Error: ${data.error}`;
  } else if (data.verification?.isIdentical) {
    resultDiv.textContent = `Verification passed! Confidence: ${data.verification.confidence}`;
  } else {
    resultDiv.textContent = `Verification failed.`;
  }
};
