// Camera module for webcam capture
let stream = null;
let video = null;
let canvas = null;

export async function startCamera() {
    try {
        video = document.getElementById('webcam');
        canvas = document.getElementById('canvas');
        
        if (!video || !canvas) {
            throw new Error('Video or canvas element not found');
        }

        // Request access to the user's camera
        stream = await navigator.mediaDevices.getUserMedia({ 
            video: { 
                facingMode: 'user',
                width: { ideal: 1280 },
                height: { ideal: 720 }
            },
            audio: false 
        });
        
        video.srcObject = stream;
        
        // Wait for video to be ready
        await new Promise((resolve) => {
            video.onloadedmetadata = () => {
                resolve();
            };
        });
        
        await video.play();
        
        console.log('Camera started successfully');
    } catch (error) {
        console.error('Error starting camera:', error);
        throw error;
    }
}

export function stopCamera() {
    try {
        if (stream) {
            stream.getTracks().forEach(track => track.stop());
            stream = null;
        }
        
        if (video) {
            video.srcObject = null;
        }
        
        console.log('Camera stopped successfully');
    } catch (error) {
        console.error('Error stopping camera:', error);
        throw error;
    }
}

export function captureFrame() {
    try {
        if (!video || !canvas) {
            throw new Error('Video or canvas not initialized');
        }
        
        // Set canvas dimensions to match video
        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;
        
        // Draw the current video frame to the canvas
        const context = canvas.getContext('2d');
        context.drawImage(video, 0, 0, canvas.width, canvas.height);
        
        // Convert canvas to data URL (base64 encoded image)
        const dataUrl = canvas.toDataURL('image/jpeg', 0.9);
        
        console.log('Frame captured successfully');
        return dataUrl;
    } catch (error) {
        console.error('Error capturing frame:', error);
        throw error;
    }
}
