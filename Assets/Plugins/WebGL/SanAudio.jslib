// Áudio do jogo no navegador via WebAudio.
// Os WAVs ficam em StreamingAssets/Audio e são baixados e decodificados aqui:
// assim o build não depende do encoder AAC da Unity (que exige ffmpeg no Linux).
mergeInto(LibraryManager.library, {
  SanAudio_Init: function (basePtr) {
    var base = UTF8ToString(basePtr);
    var A = window.SanAudio = window.SanAudio || {};
    if (A.ctx) return;
    var Ctx = window.AudioContext || window.webkitAudioContext;
    if (!Ctx) { console.warn('SanAudio: sem WebAudio'); return; }
    A.ctx = new Ctx();
    A.buffers = {};
    A.sfx = A.ctx.createGain(); A.sfx.connect(A.ctx.destination);
    A.musicGain = A.ctx.createGain(); A.musicGain.gain.value = 0; A.musicGain.connect(A.ctx.destination);
    A.ambGain = A.ctx.createGain(); A.ambGain.gain.value = 0; A.ambGain.connect(A.ctx.destination);
    A.music = null; A.musicName = null; A.amb = null; A.ambName = null;

    // o navegador só libera o som depois de um clique ou tecla
    var resume = function () { if (A.ctx.state !== 'running') A.ctx.resume(); };
    ['pointerdown', 'mousedown', 'keydown', 'touchstart'].forEach(function (e) {
      document.addEventListener(e, resume, true);
    });

    A.loop = function (name, which) {
      var buf = A.buffers[name];
      if (!buf) return null;
      var src = A.ctx.createBufferSource();
      src.buffer = buf;
      src.loop = true;
      src.connect(which === 'music' ? A.musicGain : A.ambGain);
      src.start();
      return src;
    };

    A.onLoaded = function (name) {
      if (A.musicName === name && !A.music) A.music = A.loop(name, 'music');
      if (A.ambName === name && !A.amb) A.amb = A.loop(name, 'amb');
    };

    fetch(base + '/index.txt').then(function (r) { return r.text(); }).then(function (txt) {
      txt.split(',').forEach(function (name) {
        name = name.trim();
        if (!name) return;
        fetch(base + '/' + name + '.wav')
          .then(function (r) { return r.arrayBuffer(); })
          .then(function (data) {
            return new Promise(function (ok, fail) { A.ctx.decodeAudioData(data, ok, fail); });
          })
          .then(function (buf) { A.buffers[name] = buf; A.onLoaded(name); })
          .catch(function (e) { console.warn('SanAudio: falhou ' + name, e); });
      });
    }).catch(function (e) { console.warn('SanAudio: sem índice', e); });
  },

  SanAudio_Play: function (namePtr, volume, pitch, pan) {
    var A = window.SanAudio;
    if (!A || !A.ctx) return;
    var buf = A.buffers[UTF8ToString(namePtr)];
    if (!buf) return;
    var src = A.ctx.createBufferSource();
    src.buffer = buf;
    src.playbackRate.value = pitch;
    var g = A.ctx.createGain();
    g.gain.value = volume;
    src.connect(g);
    if (A.ctx.createStereoPanner) {
      var p = A.ctx.createStereoPanner();
      p.pan.value = pan;
      g.connect(p);
      p.connect(A.sfx);
    } else {
      g.connect(A.sfx);
    }
    src.start();
  },

  SanAudio_Music: function (namePtr) {
    var A = window.SanAudio;
    if (!A || !A.ctx) return;
    var name = UTF8ToString(namePtr);
    if (A.musicName === name && A.music) return;
    if (A.music) { try { A.music.stop(); } catch (e) {} }
    A.music = null;
    A.musicName = name;
    A.music = A.loop(name, 'music');
  },

  SanAudio_MusicVolume: function (volume) {
    var A = window.SanAudio;
    if (!A || !A.ctx) return;
    A.musicGain.gain.setTargetAtTime(volume, A.ctx.currentTime, 0.05);
  },

  SanAudio_Ambience: function (namePtr, volume) {
    var A = window.SanAudio;
    if (!A || !A.ctx) return;
    var name = UTF8ToString(namePtr);
    A.ambGain.gain.setTargetAtTime(volume, A.ctx.currentTime, 0.2);
    if (A.ambName === name && A.amb) return;
    if (A.amb) { try { A.amb.stop(); } catch (e) {} }
    A.amb = null;
    A.ambName = name;
    A.amb = name ? A.loop(name, 'amb') : null;
  },

  SanAudio_Duration: function (namePtr) {
    var A = window.SanAudio;
    if (!A || !A.buffers) return 0;
    var buf = A.buffers[UTF8ToString(namePtr)];
    return buf ? buf.duration : 0;
  }
});
