using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {
    private new Rigidbody rigidbody;
    private Vector3 velocity;

	private void Start() {
		rigidbody = GetComponent<Rigidbody>();
	}

	private void Update() {
		velocity = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized * 10;
	}

	private void FixedUpdate() {
		rigidbody.MovePosition(rigidbody.position + velocity * Time.fixedDeltaTime);
	}
}
