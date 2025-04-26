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
            if (!makeToggle && ControllerInputPoller.instance.leftControllerIndexFloat >= 0.5f)
            {
                makeToggle = true;
                MakeClone();
            }

            if (!deleteToggle && ControllerInputPoller.instance.rightControllerIndexFloat >= 0.5f)
            {
                deleteToggle = true;
                Destroy(GetLastClone());
                Clones.RemoveAt(Clones.Count - 1);
            }

            if (ControllerInputPoller.instance.leftControllerIndexFloat >= 0.5f && ControllerInputPoller.instance.rightControllerIndexFloat >= 0.5f)
            {
                foreach (var clone in Clones)
                {
                    Destroy(clone);
                }

                Clones.Clear();
            }

            //rst toggles

            if (ControllerInputPoller.instance.leftControllerIndexFloat < 0.5f)
                makeToggle = false;

            if (ControllerInputPoller.instance.rightControllerIndexFloat < 0.5f)
                deleteToggle = false;
        }

        GameObject MakeClone()
        {
            var currentClone = GameObject.Instantiate(GorillaTagger.Instance.offlineVRRig.gameObject);
            currentClone.GetComponent<VRRig>().enabled = false;
            currentClone.GetComponent<VRRigReliableState>().enabled = false;
            currentClone.GetComponent<GorillaIK>().enabled = false;
            currentClone.GetComponent<RigContainer>().enabled = false;
            currentClone.GetComponent<VRRigEvents>().enabled = false;
            currentClone.GetComponent<GamePlayer>().enabled = false;
            currentClone.GetComponent<MonkeBallPlayer>().enabled = false;
            currentClone.GetComponent<CosmeticRefRegistry>().enabled = false;
            currentClone.GetComponent<XRaySkeleton>().enabled = false;
            currentClone.GetComponent("OwnershipGaurd").Destroy();
            currentClone.transform.Find("VR Constraints").gameObject.Destroy();
            currentClone.transform.Find("Holdables").gameObject.Destroy();
            currentClone.transform.Find("RigAnchor/rig/bodySlideAudio").gameObject.Destroy();
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

        GameObject GetLastClone()
        {
            return Clones[Clones.Count - 1];
        }








        //stolen audio shit cuz im not bothered to figure out why system.media doesnt exist
        private AudioClip LoadWavFromBytes(byte[] fileBytes, string clipName)
        {
            if (System.Text.Encoding.ASCII.GetString(fileBytes, 0, 4) != "RIFF" ||
                System.Text.Encoding.ASCII.GetString(fileBytes, 8, 4) != "WAVE")
            {
                Debug.LogError("Invalid WAV file");
                return null;
            }

            int channels = System.BitConverter.ToInt16(fileBytes, 22);
            int sampleRate = System.BitConverter.ToInt32(fileBytes, 24);
            int byteRate = System.BitConverter.ToInt32(fileBytes, 28);
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

            if (dataIndex == -1)
            {
                Debug.LogError("WAV file has no data chunk");
                return null;
            }

            int sampleCount = (fileBytes.Length - dataIndex) / (bitsPerSample / 8);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                if (bitsPerSample == 16)
                {
                    short sample = System.BitConverter.ToInt16(fileBytes, dataIndex + i * 2);
                    samples[i] = sample / 32768.0f;
                }
                else
                {
                    Debug.LogError("Only 16-bit WAVs are supported");
                    return null;
                }
            }

            AudioClip audioClip = AudioClip.Create(clipName, sampleCount / channels, channels, sampleRate, false);
            audioClip.SetData(samples, 0);
            return audioClip;
        }
    }
}
