﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;

public class FocusSquare : MonoBehaviour {

	public enum FocusState {
		Initializing,
		Finding,
		Found
	}

	public GameObject findingSquare;
	public GameObject foundSquare;

	//for editor version
	public float maxRayDistance = 30.0f;
	public LayerMask collisionLayerMask;
	public float findingSquareDist = 0.5f;

	private FocusState squareState;
	public FocusState SquareState { 
		get {
			return squareState;
		}
		set {
			squareState = value;
			foundSquare.SetActive (squareState == FocusState.Found);
			findingSquare.SetActive (squareState != FocusState.Found);
		} 
	}

	bool trackingInitialized;

	// Use this for initialization
	void Start () {
		SquareState = FocusState.Initializing;
		trackingInitialized = true;
	}


	bool HitTestWithResultType (ARPoint point, ARHitTestResultType resultTypes)
	{
		List<ARHitTestResult> hitResults = UnityARSessionNativeInterface.GetARSessionNativeInterface ().HitTest (point, resultTypes);
		if (hitResults.Count > 0) {
			foreach (var hitResult in hitResults) {
				Debug.Log ("Another arkitka");
				foundSquare.transform.position = UnityARMatrixOps.GetPosition (hitResult.worldTransform);
				foundSquare.transform.rotation = UnityARMatrixOps.GetRotation (hitResult.worldTransform);
				Debug.Log (string.Format ("x:{0:0.######} y:{1:0.######} z:{2:0.######}", foundSquare.transform.position.x, foundSquare.transform.position.y, foundSquare.transform.position.z));
				return true;
			}
		}
		return false;
	}

	// Update is called once per frame
	void Update () {

		//use center of screen for focusing
		Vector3 center = new Vector3(Screen.width/2, Screen.height/2, findingSquareDist);

		#if UNITY_EDITOR
		Ray ray = Camera.main.ScreenPointToRay (center);
		RaycastHit hit;

		//we'll try to hit one of the plane collider gameobjects that were generated by the plugin
		//effectively similar to calling HitTest with ARHitTestResultType.ARHitTestResultTypeExistingPlaneUsingExtent
		if (Physics.Raycast (ray, out hit, maxRayDistance, collisionLayerMask)) {
			//we're going to get the position from the contact point
			foundSquare.transform.position = hit.point;
			Debug.Log (string.Format ("x:{0:0.######} y:{1:0.######} z:{2:0.######}", foundSquare.transform.position.x, foundSquare.transform.position.y, foundSquare.transform.position.z));

			//and the rotation from the transform of the plane collider
			SquareState = FocusState.Found;
			foundSquare.transform.rotation = hit.transform.rotation;
			return;
		}


		#else
		var screenPosition = Camera.main.ScreenToViewportPoint(center);
		ARPoint point = new ARPoint {
			x = screenPosition.x,
			y = screenPosition.y
		};

		// prioritize reults types
		ARHitTestResultType[] resultTypes = {
			ARHitTestResultType.ARHitTestResultTypeExistingPlaneUsingExtent, 
			// if you want to use infinite planes use this:
			//ARHitTestResultType.ARHitTestResultTypeExistingPlane,
			//ARHitTestResultType.ARHitTestResultTypeHorizontalPlane, 
			//ARHitTestResultType.ARHitTestResultTypeFeaturePoint
		}; 

		foreach (ARHitTestResultType resultType in resultTypes)
		{
			if (HitTestWithResultType (point, resultType))
			{
				SquareState = FocusState.Found;
				return;
			}
		}

		#endif

		//if you got here, we have not found a plane, so if camera is facing below horizon, display the focus "finding" square
		if (trackingInitialized) {
			SquareState = FocusState.Finding;

			//check camera forward is facing downward
			if (Vector3.Dot(Camera.main.transform.forward, Vector3.down) > 0)
			{

				//position the focus finding square a distance from camera and facing up
				findingSquare.transform.position = Camera.main.ScreenToWorldPoint(center);

				//vector from camera to focussquare
				Vector3 vecToCamera = findingSquare.transform.position - Camera.main.transform.position;

				//find vector that is orthogonal to camera vector and up vector
				Vector3 vecOrthogonal = Vector3.Cross(vecToCamera, Vector3.up);

				//find vector orthogonal to both above and up vector to find the forward vector in basis function
				Vector3 vecForward = Vector3.Cross(vecOrthogonal, Vector3.up);


				findingSquare.transform.rotation = Quaternion.LookRotation(vecForward,Vector3.up);

			}
			else
			{
				//we will not display finding square if camera is not facing below horizon
				findingSquare.SetActive(false);
			}

		}

	}


}
