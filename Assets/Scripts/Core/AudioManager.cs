using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace Sanatorio
{
    /// <summary>
    /// Música e efeitos. Os WAVs ficam em StreamingAssets/Audio:
    /// - no navegador, tocam via WebAudio (Plugins/WebGL/SanAudio.jslib), sem
    ///   passar pelo importador da Unity (que para WebGL exige ffmpeg);
    /// - no editor, são carregados com UnityWebRequest e tocam em AudioSources.
    /// Os efeitos têm um "espacial falso": volume e pan pela distância da câmera.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }

#if UNITY_WEBGL && !UNITY_EDITOR
        private const bool Web = true;
        [DllImport("__Internal")] private static extern void SanAudio_Init(string baseUrl);
        [DllImport("__Internal")] private static extern void SanAudio_Play(string name, float volume, float pitch, float pan);
        [DllImport("__Internal")] private static extern void SanAudio_Music(string name);
        [DllImport("__Internal")] private static extern void SanAudio_MusicVolume(float volume);
        [DllImport("__Internal")] private static extern void SanAudio_Ambience(string name, float volume);
        [DllImport("__Internal")] private static extern float SanAudio_Duration(string name);
#else
        private const bool Web = false;
        private static void SanAudio_Init(string baseUrl) { }
        private static void SanAudio_Play(string name, float volume, float pitch, float pan) { }
        private static void SanAudio_Music(string name) { }
        private static void SanAudio_MusicVolume(float volume) { }
        private static void SanAudio_Ambience(string name, float volume) { }
        private static float SanAudio_Duration(string name) => 0f;
#endif

        private const int Voices = 28;
        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private AudioSource[] pool;
        private int next;
        private AudioSource music;
        private AudioSource ambience;
        private string musicName;
        private string ambienceName;
        private Coroutine fade;
        private float musicTarget = 1f;
        private float musicLevel;
        private float sentMusicVolume = -1f;

        private static string BasePath => Application.streamingAssetsPath + "/Audio";

        public static AudioManager Ensure()
        {
            if (I == null)
            {
                GameObject go = new GameObject("Audio");
                DontDestroyOnLoad(go);
                I = go.AddComponent<AudioManager>();
                I.Build();
            }

            return I;
        }

        private void Build()
        {
            if (Web)
            {
                SanAudio_Init(BasePath);
                return;
            }

            pool = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                pool[i] = gameObject.AddComponent<AudioSource>();
                pool[i].playOnAwake = false;
            }

            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            ambience = gameObject.AddComponent<AudioSource>();
            ambience.loop = true;
            StartCoroutine(LoadAll());
        }

        /// <summary>Editor / desktop: carrega todos os WAVs da pasta.</summary>
        private IEnumerator LoadAll()
        {
            if (!Directory.Exists(BasePath)) yield break;
            foreach (string file in Directory.GetFiles(BasePath, "*.wav"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                using (UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip("file://" + file, AudioType.WAV))
                {
                    yield return req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        AudioClip c = DownloadHandlerAudioClip.GetContent(req);
                        c.name = name;
                        clips[name] = c;
                        if (name == musicName && !music.isPlaying) { music.clip = c; music.Play(); }
                        if (name == ambienceName && !ambience.isPlaying) { ambience.clip = c; ambience.Play(); }
                    }
                    else
                    {
                        Debug.LogWarning($"[Audio] {name}: {req.error}");
                    }
                }
            }
        }

        private void Update()
        {
            musicLevel = Mathf.MoveTowards(musicLevel, musicTarget * SaveData.MusicVolume, Time.unscaledDeltaTime);
            if (Web)
            {
                if (Mathf.Abs(musicLevel - sentMusicVolume) > 0.005f)
                {
                    sentMusicVolume = musicLevel;
                    SanAudio_MusicVolume(musicLevel);
                }

                return;
            }

            music.volume = musicLevel;
            ambience.volume = 0.5f * SaveData.SfxVolume;
        }

        /// <summary>Toca um efeito "no mundo": mais baixo e de lado quando longe.</summary>
        public static void PlayAt(string clip, Vector2 pos, float volume = 1f, float pitchJitter = 0.06f)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Play(clip, volume, pitchJitter);
                return;
            }

            Vector2 d = pos - (Vector2)cam.transform.position;
            float att = Mathf.Clamp01(1f - d.magnitude / 20f);
            if (att <= 0.02f) return;
            Play(clip, volume * att * att, pitchJitter, Mathf.Clamp(d.x / 12f, -0.8f, 0.8f));
        }

        public static void Play(string clip, float volume = 1f, float pitchJitter = 0f, float pan = 0f)
        {
            AudioManager a = Ensure();
            float vol = Mathf.Clamp01(volume) * SaveData.SfxVolume;
            float pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            if (Web)
            {
                SanAudio_Play(clip, vol, pitch, pan);
                return;
            }

            if (!a.clips.TryGetValue(clip, out AudioClip c)) return;
            AudioSource s = a.pool[a.next];
            a.next = (a.next + 1) % Voices;
            s.clip = c;
            s.volume = vol;
            s.pitch = pitch;
            s.panStereo = pan;
            s.Play();
        }

        public static void Music(string clip, float volume = 1f)
        {
            AudioManager a = Ensure();
            a.musicTarget = volume;
            if (a.musicName == clip) return;
            a.musicName = clip;
            a.musicLevel = 0f;
            if (Web)
            {
                SanAudio_MusicVolume(0f);
                a.sentMusicVolume = 0f;
                SanAudio_Music(clip);
                return;
            }

            a.music.Stop();
            if (a.clips.TryGetValue(clip, out AudioClip c))
            {
                a.music.clip = c;
                a.music.Play();
            }
        }

        public static void MusicVolume(float target) => Ensure().musicTarget = target;

        public static void StopMusic() => Ensure().musicTarget = 0f;

        public static void Ambience(string clip)
        {
            AudioManager a = Ensure();
            if (a.ambienceName == clip) return;
            a.ambienceName = clip;
            if (Web)
            {
                SanAudio_Ambience(clip, 0.5f * SaveData.SfxVolume);
                return;
            }

            if (a.clips.TryGetValue(clip, out AudioClip c))
            {
                a.ambience.clip = c;
                a.ambience.Play();
            }
        }

        public static void StopAmbience()
        {
            AudioManager a = Ensure();
            a.ambienceName = null;
            if (Web) SanAudio_Ambience("", 0f);
            else a.ambience.Stop();
        }

        /// <summary>Toca uma música de evento por cima, abaixando a trilha.</summary>
        public static void Sting(string clip, float volume = 1f)
        {
            AudioManager a = Ensure();
            float length = Web ? SanAudio_Duration(clip) : a.clips.TryGetValue(clip, out AudioClip c) ? c.length : 0f;
            Play(clip, volume);
            if (length <= 0f) return;
            if (a.fade != null) a.StopCoroutine(a.fade);
            a.fade = a.StartCoroutine(a.Duck(length));
        }

        private IEnumerator Duck(float seconds)
        {
            float before = musicTarget;
            musicTarget = 0.15f;
            yield return new WaitForSecondsRealtime(seconds);
            musicTarget = before;
            fade = null;
        }
    }
}
