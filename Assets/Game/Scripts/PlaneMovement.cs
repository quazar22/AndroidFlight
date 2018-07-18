using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlaneMovement : MonoBehaviour {

	// these are the values of acceleration
	public float accX, accZ;
    public float yaw = 0;
	// This is the rigid body of the plane
	Rigidbody rb;
	// this is the speed of the plane (changed by a slider)
	public float speed;
	public float rotSpeed;
    public Joystick joystick;
	//threshold used to apply the rotation 
	public float thHold =0.2f;
    public float xthHold = 0.2f;
    // this is the reference value to have the phone in a comfortable position
    public float refZ=0.2f;
    public GameObject Bullet;
    public FireButton fire;
    Transform firepos;
    private Stopwatch sw;
	// THIS IS USED TO DEBUG THE ACCELERATION (VALUES BETWEEN -1 AND 1)
	public bool debugON=true;
	[Range(-1,1)]
	public float debugAccX;
	[Range(-1,1)]
	public float debugAccZ;

	void Start () {
		rb = GetComponent<Rigidbody> ();
        firepos = transform.Find("BulletPos");
        sw = new Stopwatch();
        sw.Start();
        Color color = new Color();
        color.a = 0.0f;
        joystick.bgImg.color = color;
        joystick.joystickimg.color = color;
        joystick.enabled = false;
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            Fire();
        }
        if (fire.getIsPressed() && sw.ElapsedMilliseconds > 150) {
            Fire();
            sw.Reset();
            sw.Start();
        }
    }

    public void SetJoystick()
    {
        Color color = new Color();
        if (joystick.enabled) {
            color.a = 0.0f;
        } else {
            color.a = 1.0f; color.b = 1.0f; color.g = 1.0f; color.r = 1.0f;
        }
        joystick.bgImg.color = color;
        joystick.joystickimg.color = color;
        joystick.enabled = !joystick.enabled;
    }

    void FixedUpdate () 
	{
		rb.velocity = transform.forward*speed;


        if (joystick.enabled)
        {
            accX = -joystick.Horizontal();
            accZ = joystick.Vertical();
            rb.AddRelativeTorque(new Vector3(accZ * rotSpeed, 0, accX * (rotSpeed + 150)));
        }
        else {
            accX = -Input.acceleration.x;
            accZ = -Input.acceleration.z;
        }

        if(debugON)
        {
            accX = -debugAccX;
            accZ = -debugAccZ;
        }

        rb.AddForce (transform.forward * speed);
        if (!joystick.enabled)
        {
            if (accZ < 0.5f)
            { //up
                if (accZ <= 0.5f && accZ > 0.4f)
                {
                    rb.AddRelativeTorque(new Vector3((-0.1f) * rotSpeed * 3.0f, 0, 0));
                }
                else if (accZ <= 0.4f && accZ > 0.3f)
                {
                    rb.AddRelativeTorque(new Vector3((-0.2f) * rotSpeed * 3.0f, 0, 0));
                }
                else if (accZ <= 0.3f && accZ > 0.2f)
                {
                    rb.AddRelativeTorque(new Vector3((-0.3f) * rotSpeed * 3.0f, 0, 0));
                }
                else if (accZ <= 0.2f && accZ > 0.1f)
                {
                    rb.AddRelativeTorque(new Vector3((-0.4f) * rotSpeed * 3.0f, 0, 0));
                }
                else if (accZ <= 0.1f && accZ > 0.0f)
                {
                    rb.AddRelativeTorque(new Vector3((-0.5f) * rotSpeed * 3.0f, 0, 0));
                }
                else if (accZ <= 0.0f && accZ > -0.1f)
                {
                    rb.AddRelativeTorque(new Vector3((-0.6f) * rotSpeed * 3.0f, 0, 0));
                }
                else if (accZ <= -0.1f)
                {
                    rb.AddRelativeTorque(new Vector3((-0.7f) * rotSpeed * 3.0f, 0, 0));
                }
            }
            if (accZ >= 0.7f)
            { //down
                if (accZ <= 0.8f && accZ >= 0.7f)
                    rb.AddRelativeTorque(new Vector3((0.4f) * rotSpeed, 0, 0));
                else if (accZ <= 0.9f && accZ > 0.8f)
                    rb.AddRelativeTorque(new Vector3((0.7f) * rotSpeed, 0, 0));
                else if (accZ <= 1.0f && accZ > 0.9f)
                    rb.AddRelativeTorque(new Vector3((1.0f) * rotSpeed, 0, 0));
                else if(accZ > 1.0f)
                    rb.AddRelativeTorque(new Vector3((1.0f) * rotSpeed, 0, 0));
            }
            if (Mathf.Abs(accX) > xthHold)
            {
                rb.AddRelativeTorque(new Vector3(0, 0, accX * (rotSpeed + 100)));
            }
        }

        //rb.AddForce(0, -900.8f, 0); //harsher gravity?


    }

    public void Fire()
    {
        Instantiate(Bullet, firepos.position, Quaternion.identity);
    }

    public void updateSpeed(Slider sl)
	{
		speed = sl.value;
	}

    public void SetLeftYaw(float yaw)
    {
        this.yaw = -yaw;
    }

    public void SetRightYaw(float yaw)
    {
        this.yaw = yaw;
    }

}
