﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace DragonBone
{
	[ExecuteInEditMode]
	public class DragonBoneArmature : MonoBehaviour {

		[Range(0.0001f,1f)]
		public float zSpace = 0.001f;
		[SerializeField]
		private bool m_FlipX;
		[SerializeField]
		private bool m_FlipY;

		public Slot[] slots;
		public SpriteFrame[] updateFrames;
		public SpriteMesh[] updateMeshs;
		public Renderer[] attachments;
		public Material[] materials;
		public TextureFrame[] textureFrames;

		private List<Slot> m_OrderSlots = new List<Slot>();

		private Animator m_animator;
		public Animator aniamtor{
			get { 
				if(m_animator==null) m_animator = gameObject.GetComponent<Animator>();
				return m_animator;
			} 
		}

		[SerializeField]
		protected string m_SortingLayerName = "Default";
		/// <summary>
		/// Name of the Renderer's sorting layer.
		/// </summary>
		public string sortingLayerName
		{
			get {
				return m_SortingLayerName;
			}
			set {
				m_SortingLayerName = value;
				foreach(Renderer r in attachments){
					if(r) {
						r.sortingLayerName = value;
						#if UNITY_EDITOR 
						if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(r);
						#endif
						SpriteFrame sf = r.GetComponent<SpriteFrame>();
						if(sf) {
							sf.sortingLayerName = value;
							#if UNITY_EDITOR 
							if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(sf);
							#endif
						}
						else {
							SpriteMesh sm = r.GetComponent<SpriteMesh>();
							if(sm) {
								sm.sortingLayerName = value;
								#if UNITY_EDITOR 
								if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(sm);
								#endif
							}
						}
					}
				}
			}
		}

		[SerializeField]
		protected int m_SortingOrder = 0;
		/// <summary>
		/// Renderer's order within a sorting layer.
		/// </summary>
		public int sortingOrder
		{
			get {
				return m_SortingOrder;
			}
			set {
				m_SortingOrder = value;
				foreach(Renderer r in attachments){
					if(r){
						r.sortingOrder = value;
						#if UNITY_EDITOR 
						if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(r);
						#endif
						SpriteFrame sf = r.GetComponent<SpriteFrame>();
						if(sf) {
							sf.sortingOrder = value;
							#if UNITY_EDITOR 
							if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(sf);
							#endif
						}
						else {
							SpriteMesh sm = r.GetComponent<SpriteMesh>();
							if(sm) {
								sm.sortingOrder = value;
								#if UNITY_EDITOR 
								if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(sm);
								#endif
							}
						}
					}
				}
			}
		}


		public bool flipX{
			get { return m_FlipX; }
			set {
				#if !UNITY_EDITOR
				if(m_FlipX == value) return;
				#endif
				m_FlipX =  value;

				transform.Rotate(0f,180f,0f);

				Vector3 v = transform.localEulerAngles;
				v.x = ClampAngle(v.x,-360f,360f);
				v.y = ClampAngle(v.y,-360f,360f);
				v.z = ClampAngle(v.z,-720f,720f);
				transform.localEulerAngles=v;

				#if UNITY_EDITOR 
				if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(transform);
				#endif
				ResetSlotZOrder();
			}
		}

		public bool flipY{
			get { return m_FlipY; }
			set {
				#if !UNITY_EDITOR
				if(m_FlipY == value) return;
				#endif
				m_FlipY =  value;
				transform.Rotate(180f,0f,0f);

				Vector3 v = transform.localEulerAngles;
				v.x = ClampAngle(v.x,-360f,360f);
				v.y = ClampAngle(v.y,-360f,360f);
				v.z = ClampAngle(v.z,-720f,720f);
				transform.localEulerAngles=v;

				#if UNITY_EDITOR 
				if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(transform);
				#endif
				ResetSlotZOrder();
			}
		}

		float ClampAngle(float angle,float min ,float max){
			if (angle<90 || angle>270){       // if angle in the critic region...
				if (angle>180) angle -= 360;  // convert all angles to -180..+180
				if (max>180) max -= 360;
				if (min>180) min -= 360;
			}
			angle = Mathf.Clamp(angle, min, max);
			return angle;
		}

		// Update is called once per frame
		void Update () {
			#if UNITY_EDITOR
			if(Application.isPlaying){
				if(aniamtor!=null && aniamtor.enabled)
				{
					UpdateArmature();
				}
			}
			else
			{
				if(aniamtor!=null)
				{
					UpdateArmature();
				}
			}
			#else
			if(aniamtor!=null && aniamtor.enabled)
			{
				UpdateArmature();
			}
			#endif
		}

		//after animation frame
		void LateUpdate(){
			if(aniamtor!=null && m_OrderSlots.Count>0)
			{
				int len = slots.Length;
				Slot[] newSlots = new Slot[len];
				for ( int i = 0; i < m_OrderSlots.Count ;++i ) {
					Slot slot = m_OrderSlots[ i ];
					int newIdx = slot.zOrder+slot.z;
					newSlots[ newIdx ] = slot;
					slots[ slot.zOrder ]._zOrderValid = true;
				}
				int pos = 0;
				for ( int i = 0; i< len; ++i ) {
					Slot newSlot = newSlots[ i ];
					if ( newSlot==null ) {
						for ( ; pos != len; ) {
							if ( !slots[ pos ]._zOrderValid ) {
								newSlots[ i ] = slots[ pos ];
								++pos;
								break;
							} else ++pos;
						}
					}
				}

				//set new order
				float zoff = m_FlipX || m_FlipY ? 1f : -1f;
				if(m_FlipX && m_FlipY) zoff = -1f;
				zoff*=zSpace;
				for ( int j = 0; j < len; ++j ) {
					Slot slot = newSlots[j];
					if(slot){
						Vector3 v = slot.transform.localPosition;
						v.z = zoff*j+zoff*0.00001f;
						slot.transform.localPosition = v;
						slot._zOrderValid = false;
					}
				}

				m_OrderSlots.Clear();
			}
		}

		/// <summary>
		/// update
		/// </summary>
		public void UpdateArmature(){
			int len = updateFrames.Length;
			for(int i=0;i<len;++i){
				SpriteFrame frame = updateFrames[i];
				if(frame&&frame.isActiveAndEnabled) frame.UpdateFrame();
			}

			len = updateMeshs.Length;
			for(int i=0;i<len;++i){
				SpriteMesh mesh = updateMeshs[i];
				if(mesh&&mesh.isActiveAndEnabled) mesh.UpdateMesh();
			}

			len = slots.Length;
			for(int i=0;i<len;++i){
				Slot slot = slots[i];
				if(slot && slot.isActiveAndEnabled){
					slot.UpdateSlot();
				}
			}
		}

		/// <summary>
		/// Resets the slot Z order.
		/// </summary>
		public void ResetSlotZOrder(){
			float tempZ = m_FlipX || m_FlipY ? 1f : -1f;
			if(m_FlipX && m_FlipY) tempZ = -1f;

			tempZ*=zSpace;
			int len = slots.Length;
			for(int i=0;i<len;++i){
				Slot slot = slots[i];
				if(slot){
					Vector3 v = slot.transform.localPosition;
					v.z = tempZ*slot.zOrder+tempZ*0.00001f;
					slot.transform.localPosition = v;
					#if UNITY_EDITOR 
					if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(slot.transform);
					#endif
				}
			}
		}

		/// <summary>
		/// slot call this function
		/// </summary>
		/// <param name="slot">Slot.</param>
		public void UpdateSlotZOrder(Slot slot){
			m_OrderSlots.Add(slot);
		}

	}
}
