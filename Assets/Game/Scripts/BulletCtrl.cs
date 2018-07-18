using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletCtrl : MonoBehaviour {

    public float speed;
    Rigidbody rb;
    GameObject planeObj;
    //GameObject firepos;
	// Use this for initialization
	void Start () {
        rb = GetComponent<Rigidbody>();
        //firepos = GameObject.Find("BulletPos");
        planeObj = GameObject.Find("planeObj");
        //rb.velocity = speed;
        rb.velocity = planeObj.transform.forward * speed;
    }

    // Update is called once per frame
    void Update () {
        //rb.velocity = speed;
        rb.velocity = planeObj.transform.forward * speed;
    }

    //2D
    //private void OnCollisionEnter2D(Collision2D collision)
    //{
    //    if (collision.gameObject.tag == "CRATE") {
    //        Destroy(collision.gameObject);
    //        Destroy(gameObject);
    //    }
    //}
    private void OnCollisionEnter(Collision collision)
    {

    }
}
