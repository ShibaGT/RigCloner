using BepInEx;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace RigCloner
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        bool makeToggle;
        bool deleteToggle;
        List<GameObject> Clones = new List<GameObject>();

        void Update()
        {
            bool keyboardB = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.bKey.wasPressedThisFrame;
            
            if ((!makeToggle && ControllerInputPoller.instance.leftControllerIndexFloat >= 0.5f) || keyboardB)
            {
                makeToggle = true;
                MakeClone();
            }

            if (!deleteToggle && ControllerInputPoller.instance.rightControllerIndexFloat >= 0.5f)
            {
                deleteToggle = true;
                if (Clones.Count > 0)
                {
                    Destroy(GetLastClone());
                    Clones.RemoveAt(Clones.Count - 1);
                }
            }

            if (ControllerInputPoller.instance.leftControllerIndexFloat >= 0.5f && ControllerInputPoller.instance.rightControllerIndexFloat >= 0.5f)
            {
                foreach (var clone in Clones) Destroy(clone);
                Clones.Clear();
            }

            if (ControllerInputPoller.instance.leftControllerIndexFloat < 0.5f) makeToggle = false;
            if (ControllerInputPoller.instance.rightControllerIndexFloat < 0.5f) deleteToggle = false;
        }

        GameObject MakeClone()
        {
            var currentClone = Instantiate(GorillaTagger.Instance.offlineVRRig.gameObject);
            
            if (currentClone.GetComponent<VRRig>()) currentClone.GetComponent<VRRig>().enabled = false;
            if (currentClone.GetComponent<VRRigReliableState>()) currentClone.GetComponent<VRRigReliableState>().enabled = false;
            if (currentClone.GetComponent<GorillaIK>()) currentClone.GetComponent<GorillaIK>().enabled = false;
            if (currentClone.GetComponent<RigContainer>()) currentClone.GetComponent<RigContainer>().enabled = false;
            if (currentClone.GetComponent<VRRigEvents>()) currentClone.GetComponent<VRRigEvents>().enabled = false;
            if (currentClone.GetComponent<GamePlayer>()) currentClone.GetComponent<GamePlayer>().enabled = false;
            if (currentClone.GetComponent<MonkeBallPlayer>()) currentClone.GetComponent<MonkeBallPlayer>().enabled = false;
            if (currentClone.GetComponent<CosmeticRefRegistry>()) currentClone.GetComponent<CosmeticRefRegistry>().enabled = false;
            if (currentClone.GetComponent<XRaySkeleton>()) currentClone.GetComponent<XRaySkeleton>().enabled = false;

            var guard = currentClone.GetComponent("OwnershipGuard");
            if (guard == null) guard = currentClone.GetComponent("OwnershipGaurd");
            if (guard != null) Destroy(guard);

            var constraints = currentClone.transform.Find("VR Constraints");
            if (constraints != null) Destroy(constraints.gameObject);

            var holdables = currentClone.transform.Find("Holdables");
            if (holdables != null) Destroy(holdables.gameObject);

            var audio = currentClone.transform.Find("RigAnchor/rig/bodySlideAudio");
            if (audio != null) Destroy(audio.gameObject);

            currentClone.transform.position = GorillaTagger.Instance.offlineVRRig.transform.position;
            currentClone.transform.rotation = GorillaTagger.Instance.offlineVRRig.transform.rotation;
            Clones.Add(currentClone);

            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream("RigCloner.aa.dbz-teleport.wav"))
            {
                if (stream != null)
                {
                    byte[] wavData = new byte[stream.Length];
                    stream.Read(wavData, 0, wavData.Length);
                    AudioClip clip = LoadWavFromBytes(wavData, "dbz-teleport");
                    if (clip != null)
                    {
                        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
                        audioSource.clip = clip;
                        audioSource.Play();
                    }
                }
            }

            return currentClone;
        }

        GameObject GetLastClone() => Clones[Clones.Count - 1];

        private AudioClip LoadWavFromBytes(byte[] fileBytes, string clipName)
        {
            if (System.Text.Encoding.ASCII.GetString(fileBytes, 0, 4) != "RIFF" || System.Text.Encoding.ASCII.GetString(fileBytes, 8, 4) != "WAVE") return null;
            int channels = System.BitConverter.ToInt16(fileBytes, 22);
            int sampleRate = System.BitConverter.ToInt32(fileBytes, 24);
            int bitsPerSample = System.BitConverter.ToInt16(fileBytes, 34);

            int dataIndex = -1;
            for (int i = 12; i < fileBytes.Length - 4; i++)
            {
                if (System.Text.Encoding.ASCII.GetString(fileBytes, i, 4) == "data")
                {
                    dataIndex = i + 8;
                    break;
                }
            }

            if (dataIndex == -1) return null;

            int sampleCount = (fileBytes.Length - dataIndex) / (bitsPerSample / 8);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                if (bitsPerSample == 16)
                {
                    short sample = System.BitConverter.ToInt16(fileBytes, dataIndex + i * 2);
                    samples[i] = sample / 32768.0f;
                }
                else return null;
            }

            AudioClip audioClip = AudioClip.Create(clipName, sampleCount / channels, channels, sampleRate, false);
            audioClip.SetData(samples, 0);
            return audioClip;
        }
    }
}