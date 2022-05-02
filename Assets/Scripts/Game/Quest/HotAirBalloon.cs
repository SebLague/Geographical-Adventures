using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeoGame.Quest
{
	public class HotAirBalloon : MonoBehaviour
	{

		[Header("Settings")]
		public float heightAboveSurface;
		public float fadeSpeed;

		public float bobSpeed;
		public float bobAmplitude;
		public float spawnSpeed;

		[Header("References")]
		public SphereCollider interactionSphere;
		public MeshRenderer balloonRenderer;
		public MeshRenderer basketRenderer;
		public ParticleSystem fireParticles;
		public ParticleSystemRenderer fireParticlesR;
		public Transform fireLightSource;
		public AudioSource interactAudio;

		// Private stuff
		float bobTimeOffset;
		Vector3 startPos;
		Vector3 targetAnchorPos;
		Vector3 currentAnchorPos;
		float fadeT;
		float spawnT;
		Transform player;
		System.Action onInteractedCallback;
		bool interactionComplete;
		int firePosShaderID;

		public void Init(Transform player, Vector3 surfacePosition, System.Action onInteractedCallback, Texture2D flag)
		{
			this.onInteractedCallback = onInteractedCallback;
			this.player = player;
			bobTimeOffset = Random.value * Mathf.PI;
			Vector3 up = surfacePosition.normalized;
			targetAnchorPos = surfacePosition + up * heightAboveSurface;
			startPos = surfacePosition;
			transform.position = targetAnchorPos;
			transform.up = up;

			balloonRenderer.material.mainTexture = flag;
			basketRenderer.sharedMaterial = new Material(basketRenderer.sharedMaterial);
			fireParticlesR.sharedMaterial = new Material(fireParticlesR.sharedMaterial);
			firePosShaderID = Shader.PropertyToID("firePos");
			transform.localScale = Vector3.zero;
		}

		void Update()
		{


			balloonRenderer.sharedMaterial.SetVector(firePosShaderID, fireLightSource.position);
			basketRenderer.sharedMaterial.SetVector(firePosShaderID, fireLightSource.position);


			spawnT += Time.deltaTime * spawnSpeed;
			currentAnchorPos = Vector3.Lerp(startPos, targetAnchorPos, Maths.Ease.Cubic.InOut(spawnT));
			transform.localScale = Vector3.one * Maths.Ease.Cubic.InOut(spawnT);
			float bobOffset = Mathf.Sin((Time.time + bobTimeOffset) * bobSpeed) * bobAmplitude;
			transform.position = currentAnchorPos + transform.up * bobOffset;

			if (interactionComplete)
			{
				//gameObject.SetActive(false);
				fadeT += Time.deltaTime * fadeSpeed;
				float alpha = Mathf.Clamp01(1 - fadeT);
				balloonRenderer.sharedMaterial.SetFloat("_Fade", alpha);
				basketRenderer.sharedMaterial.SetFloat("_Fade", alpha);
				fireParticlesR.sharedMaterial.color = new Color(1, 1, 1, alpha);

				//transform.localScale = Vector3.one * Maths.Ease.Quadratic.InOut(1-t);
				if (fadeT > 1)
				{
					gameObject.SetActive(false);
					//Destroy(gameObject);
				}
			}
			else
			{
				Vector3 interactionSphereCentre = transform.TransformPoint(interactionSphere.center);
				float playerInteractionRadius = 0.5f;

				if (Maths.Sphere.OverlapsSphere(interactionSphereCentre, interactionSphere.radius, player.position, playerInteractionRadius))
				{
					interactionComplete = true;
					interactAudio.Play();
					onInteractedCallback.Invoke();
				}
			}
		}

	}
}