// Camera functionality for face capture
window.camera = {
    currentStream: null,

    async initializeCamera(videoElementId) {
        try {
            const video = document.getElementById(videoElementId);
            if (!video) {
                console.error('Video element not found');
                return false;
            }

            // Check if mediaDevices is supported
            if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
                console.error('Camera API not supported');
                alert('Your browser does not support camera access. Please use a modern browser with HTTPS.');
                return false;
            }

            // Request camera access
            const constraints = {
                video: {
                    width: { ideal: 1280 },
                    height: { ideal: 720 },
                    facingMode: 'user'
                },
                audio: false
            };

            this.currentStream = await navigator.mediaDevices.getUserMedia(constraints);
            video.srcObject = this.currentStream;
            
            // Wait for video to be ready
            await new Promise((resolve) => {
                video.onloadedmetadata = () => {
                    video.play();
                    resolve();
                };
            });

            console.log('Camera initialized successfully');
            return true;
        } catch (error) {
            console.error('Error accessing camera:', error);
            if (error.name === 'NotAllowedError') {
                alert('Camera access denied. Please allow camera access to use this feature.');
            } else if (error.name === 'NotFoundError') {
                alert('No camera found on this device.');
            } else {
                alert('Error accessing camera: ' + error.message);
            }
            return false;
        }
    },

    capturePhoto(videoElementId, maxSizeKB = 1024) {
        try {
            const video = document.getElementById(videoElementId);
            if (!video || !video.srcObject) {
                console.error('Video not initialized');
                return null;
            }

            // Create canvas to capture frame
            const canvas = document.createElement('canvas');
            canvas.width = video.videoWidth;
            canvas.height = video.videoHeight;

            const context = canvas.getContext('2d');
            context.drawImage(video, 0, 0, canvas.width, canvas.height);

            // Convert to base64 with compression
            let quality = 0.9;
            let dataUrl = canvas.toDataURL('image/jpeg', quality);

            // Compress if needed
            while (dataUrl.length > maxSizeKB * 1024 && quality > 0.1) {
                quality -= 0.1;
                dataUrl = canvas.toDataURL('image/jpeg', quality);
            }

            console.log('Photo captured successfully');
            return dataUrl;
        } catch (error) {
            console.error('Error capturing photo:', error);
            alert('Error capturing photo: ' + error.message);
            return null;
        }
    },

    stopCamera(videoElementId) {
        try {
            const video = document.getElementById(videoElementId);
            
            if (this.currentStream) {
                this.currentStream.getTracks().forEach(track => track.stop());
                this.currentStream = null;
            }

            if (video) {
                video.srcObject = null;
            }

            console.log('Camera stopped successfully');
            return true;
        } catch (error) {
            console.error('Error stopping camera:', error);
            return false;
        }
    },

    async checkCameraPermission() {
        try {
            if (!navigator.permissions) {
                return 'prompt';
            }
            
            const permission = await navigator.permissions.query({ name: 'camera' });
            return permission.state; // 'granted', 'denied', or 'prompt'
        } catch (error) {
            console.error('Error checking camera permission:', error);
            return 'prompt';
        }
    },

    dataURLtoBlob(dataURL) {
        const arr = dataURL.split(',');
        const mime = arr[0].match(/:(.*?);/)[1];
        const bstr = atob(arr[1]);
        let n = bstr.length;
        const u8arr = new Uint8Array(n);
        while (n--) {
            u8arr[n] = bstr.charCodeAt(n);
        }
        return new Blob([u8arr], { type: mime });
    },

    detectLighting(videoElementId) {
        try {
            const video = document.getElementById(videoElementId);
            if (!video || !video.srcObject) {
                return 'unknown';
            }

            const canvas = document.createElement('canvas');
            canvas.width = video.videoWidth;
            canvas.height = video.videoHeight;

            const context = canvas.getContext('2d');
            context.drawImage(video, 0, 0, canvas.width, canvas.height);

            const imageData = context.getImageData(0, 0, canvas.width, canvas.height);
            const data = imageData.data;

            let brightness = 0;
            for (let i = 0; i < data.length; i += 4) {
                const r = data[i];
                const g = data[i + 1];
                const b = data[i + 2];
                brightness += (r + g + b) / 3;
            }

            brightness = brightness / (data.length / 4);

            if (brightness < 50) {
                return 'too-dark';
            } else if (brightness > 200) {
                return 'too-bright';
            } else {
                return 'good';
            }
        } catch (error) {
            console.error('Error detecting lighting:', error);
            return 'unknown';
        }
    }
};
