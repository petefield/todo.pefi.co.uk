window.celebrate = () => {
    // Confetti burst from both sides
    const defaults = { startVelocity: 30, spread: 360, ticks: 60, zIndex: 9999 };

    confetti({ ...defaults, particleCount: 50, origin: { x: 0.3, y: 0.6 } });
    confetti({ ...defaults, particleCount: 50, origin: { x: 0.7, y: 0.6 } });

    setTimeout(() => {
        confetti({ ...defaults, particleCount: 30, origin: { x: 0.5, y: 0.4 } });
    }, 150);

    // Applause sound using Web Audio API
    try {
        const ctx = new (window.AudioContext || window.webkitAudioContext)();
        const duration = 1.2;
        const sampleRate = ctx.sampleRate;
        const length = sampleRate * duration;
        const buffer = ctx.createBuffer(1, length, sampleRate);
        const data = buffer.getChannelData(0);

        // Generate applause-like noise with envelope
        for (let i = 0; i < length; i++) {
            const t = i / sampleRate;
            // Envelope: quick attack, sustain, fade out
            let envelope;
            if (t < 0.05) envelope = t / 0.05;
            else if (t < 0.6) envelope = 1.0;
            else envelope = Math.max(0, 1.0 - (t - 0.6) / 0.6);

            // Layered filtered noise for clapping texture
            const noise = (Math.random() * 2 - 1);
            const burstRate = 18;
            const burst = 0.5 + 0.5 * Math.sin(t * burstRate * Math.PI * 2);
            data[i] = noise * envelope * burst * 0.3;
        }

        // Bandpass filter for more realistic sound
        const source = ctx.createBufferSource();
        source.buffer = buffer;

        const bandpass = ctx.createBiquadFilter();
        bandpass.type = 'bandpass';
        bandpass.frequency.value = 3000;
        bandpass.Q.value = 0.5;

        const gain = ctx.createGain();
        gain.gain.value = 0.6;

        source.connect(bandpass);
        bandpass.connect(gain);
        gain.connect(ctx.destination);
        source.start();

        source.onended = () => ctx.close();
    } catch (e) {
        // Audio not available, confetti is enough
    }
};
