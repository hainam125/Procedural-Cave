using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player2D : MonoBehaviour {
	private new Rigidbody2D rigidbody;
	private Vector2 velocity;

	private void Start() {
		rigidbody = GetComponent<Rigidbody2D>();
	}

	private void Update() {
		velocity = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized * 10;
	}

	private void FixedUpdate() {
		rigidbody.MovePosition(rigidbody.position + velocity * Time.fixedDeltaTime);
	}
}
