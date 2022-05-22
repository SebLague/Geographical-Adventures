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
			currentAnchorPos = Vector3.Lerp(startPos, targetAnchorPos, Seb.Ease.Cubic.InOut(spawnT));
			transform.localScale = Vector3.one * Seb.Ease.Cubic.InOut(spawnT);
			float bobOffset = Mathf.Sin((Time.time + bobTimeOffset) * bobSpeed) * bobAmplitude;
			transform.position = currentAnchorPos + transform.up * bobOffset;

			if (interactionComplete)
			{
				fadeT += Time.deltaTime * fadeSpeed;
				float alpha = Mathf.Clamp01(1 - fadeT);
				balloonRenderer.sharedMaterial.SetFloat("_Fade", alpha);
				basketRenderer.sharedMaterial.SetFloat("_Fade", alpha);
				fireParticlesR.sharedMaterial.color = new Color(1, 1, 1, alpha);

				if (fadeT > 1)
				{
					gameObject.SetActive(false);
				}
			}
			else if (player != null)
			{
				CheckForPlayerCollision();
			}
		}

		void OnPlayerCollide()
		{
			interactAudio.Play();
			onInteractedCallback.Invoke();
		}


		void CheckForPlayerCollision()
		{
			Vector3 interactionSphereCentre = transform.TransformPoint(interactionSphere.center);
			float playerInteractionRadius = 0.5f;

			if (Seb.Maths.SphereOverlapsSphere(interactionSphereCentre, interactionSphere.radius, player.position, playerInteractionRadius))
			{
				interactionComplete = true;
				OnPlayerCollide();
			}
		}

	}

}