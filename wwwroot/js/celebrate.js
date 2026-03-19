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
        const duration = 1.5;
        const sampleRate = ctx.sampleRate;
        const length = sampleRate * duration;
        const buffer = ctx.createBuffer(1, length, sampleRate);
        const data = buffer.getChannelData(0);

        // Simulate many individual claps overlapping
        const numClaps = 80;
        for (let c = 0; c < numClaps; c++) {
            // Each clap starts at a random time, clustered toward the beginning
            const clapStart = Math.random() * 0.8 * sampleRate;
            const clapLen = Math.floor((0.01 + Math.random() * 0.025) * sampleRate);
            const clapVol = 0.15 + Math.random() * 0.2;

            for (let i = 0; i < clapLen && (clapStart + i) < length; i++) {
                const t = i / clapLen;
                // Sharp attack, quick decay — like a single handclap
                const env = Math.exp(-t * 12) * (1 - Math.exp(-t * 500));
                const idx = Math.floor(clapStart + i);
                data[idx] += (Math.random() * 2 - 1) * env * clapVol;
            }
        }

        // Overall envelope: fade out the whole thing
        for (let i = 0; i < length; i++) {
            const t = i / sampleRate;
            let env;
            if (t < 0.05) env = t / 0.05;
            else if (t < 0.7) env = 1.0;
            else env = Math.max(0, 1.0 - (t - 0.7) / 0.8);
            data[i] *= env;
            // Clamp
            data[i] = Math.max(-1, Math.min(1, data[i]));
        }

        const source = ctx.createBufferSource();
        source.buffer = buffer;

        // Bandpass to remove low rumble and harsh highs
        const hp = ctx.createBiquadFilter();
        hp.type = 'highpass';
        hp.frequency.value = 800;

        const lp = ctx.createBiquadFilter();
        lp.type = 'lowpass';
        lp.frequency.value = 6000;

        const gain = ctx.createGain();
        gain.gain.value = 0.8;

        source.connect(hp);
        hp.connect(lp);
        lp.connect(gain);
        gain.connect(ctx.destination);
        source.start();

        source.onended = () => ctx.close();
    } catch (e) {
        // Audio not available, confetti is enough
    }
};
