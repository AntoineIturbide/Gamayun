using UnityEngine;
using System.Collections;

namespace Gameplay.Water
{
    public class AnimateTextureTiling : MonoBehaviour
    {
        public Vector2 offset;
        public float speed;
        private Material mat;

        public MeshRenderer mr;
        private void Start()
        {
            mr.material = mat = new Material(mr.material);
        }

        void Update()
        {
            mat.SetTextureOffset("_MainTex", offset * speed * Time.time);
        }
    }
}