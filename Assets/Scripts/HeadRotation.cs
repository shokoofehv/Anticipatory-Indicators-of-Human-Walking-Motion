using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random=UnityEngine.Random;

public class HeadRotation : MonoBehaviour
{   
    public float head_orientation;
    public BodyController body;
    public Manager manager;

    Quaternion start_rotation;
    Quaternion end_rotation;
    // float rotation_progress = -1;

    private IEnumerator head_routine;
    private bool reset;
    void Start()
    {
        if (!manager.Replay)
        {
            transform.rotation = body.transform.rotation;
            head_routine = RandomRotate();
        }
    }

    void Update()
    {

        if (!manager.Replay)
        {
            // MouseControl();

            Reset();

            // RandomHeadRotation();
                
        }
        if (transform.eulerAngles.y > 180)
            head_orientation = transform.eulerAngles.y - 360;
        
        else
            head_orientation = transform.eulerAngles.y;
        
    }

    void Reset()
    {   
        reset = body.reset;

        if (reset)
        {
            // Debug.Log("Starting over head routine.");

            StopCoroutine(head_routine);
            reset = !reset;
            StartCoroutine(head_routine);
        } 
    }

    IEnumerator RandomRotate()
    {
        bool wrong_rotation = false;

        yield return new WaitForSeconds(0.7f);
        while (!reset)
        {   
            // yield return new WaitForSeconds(0.7f);
            wrong_rotation = false;

            float speed = Random.Range(0.1f, body.head_rotation_rate); 
            float rand_deg = Random.Range(-20.0f, 20.0f);

            start_rotation = body.transform.rotation;
            end_rotation = Quaternion.Euler(0, start_rotation.eulerAngles.y + rand_deg, 0);

            for (var t = 0f; t < 1 && (!reset); t += Time.deltaTime * speed)
            {

                transform.rotation = Quaternion.Slerp(start_rotation, end_rotation, t);
                
                var diff = AngleDiff(body.transform.eulerAngles.y, transform.eulerAngles.y); 
                                
                if (diff > 40)
                {
                    wrong_rotation = true;
                    start_rotation = transform.rotation;
                    end_rotation = body.transform.rotation;
                    // break;
                    // t = 0;
                    continue;
                }
                yield return null;
            }

            if (reset) continue; 
            // yield return new WaitForSeconds(0.2f);
            
            start_rotation = transform.rotation;
            end_rotation = body.transform.rotation; 

            var step = (wrong_rotation) ? Time.deltaTime * speed * 5 : Time.deltaTime * speed; 
            for (var t = 0f; t < 1 && (!reset); t += step)
            {
                transform.rotation = Quaternion.Slerp(start_rotation, end_rotation, t);
                yield return null;
            }

            wrong_rotation = false;
        }
    }

    float AngleDiff(float x, float y)
    {
        var diff1 = Math.Abs(x - y);
        var diff2 = Math.Abs(x - y - 360);
        var diff3 = Math.Abs(x - y + 360);
        return Math.Min(diff1, Math.Min(diff2, diff3));
    }

    void RandomHeadRotation()
    {
        bool wrong_rotation = false;

        for (var t = 0f; t < 0.7; t += Time.deltaTime)
            continue;

        float speed = Random.Range(0.1f, body.head_rotation_rate); 
        float rand_deg = Random.Range(-40.0f, 40.0f);

        start_rotation = body.transform.rotation;
        end_rotation = Quaternion.Euler(0, start_rotation.eulerAngles.y + rand_deg, 0);
        
        for (var t = 0f; t < 1; t += Time.deltaTime * speed)
        {
            transform.rotation = Quaternion.Slerp(start_rotation, end_rotation, t);
            
            var diff = (body.transform.eulerAngles.y > 180 && transform.eulerAngles.y < 180) ? Math.Abs(body.transform.eulerAngles.y - transform.eulerAngles.y - 360) :
                       ((body.transform.eulerAngles.y < 180 && transform.eulerAngles.y > 180) ? Math.Abs(body.transform.eulerAngles.y - transform.eulerAngles.y + 360) : Math.Abs(body.transform.eulerAngles.y - transform.eulerAngles.y));
            
            Debug.Log($"1body: {body.transform.eulerAngles.y} head: {transform.eulerAngles.y} diff: {diff} deg: {rand_deg}");
            
            if (diff > 50)
            {
                wrong_rotation = true;
                break;
            }

        }
        if (!wrong_rotation) 
            for (var t = 0f; t < 0.2; t += Time.deltaTime)
                continue;

        start_rotation = transform.rotation;
        end_rotation = body.transform.rotation; 

        var step = (wrong_rotation) ? Time.deltaTime * speed * 3 : Time.deltaTime * speed; 
        for (var t = 0f; t < 1; t += step)
        {
            Debug.Log($"2body: {body.transform.eulerAngles.y} head: {transform.eulerAngles.y} diff: {Math.Abs(body.transform.eulerAngles.y - transform.eulerAngles.y)} deg: {rand_deg}");
            transform.rotation = Quaternion.Slerp(start_rotation, end_rotation, t);
        }
    }

    // void RandomHeadRotation()
    // {
    //     float speed = Random.Range(0.1f, body.head_rotation_rate); 
    //     float rand_deg = Random.Range(-30.0f, 30.0f);
        
    //     if (rotation_progress < 1 && rotation_progress >= 0)
    //     {
    //         rotation_progress += Time.deltaTime * speed * 4;
    //         transform.rotation = Quaternion.Slerp(start_rotation, end_rotation, rotation_progress);
    //         if (Math.Abs(body.transform.rotation.eulerAngles.y - end_rotation.eulerAngles.y) > 60)
    //         {
    //             rotation_progress = 0;
    //             start_rotation = body.transform.rotation;
    //             end_rotation = Quaternion.Euler(0, start_rotation.eulerAngles.y + rand_deg, 0);
    //         }
    //     }

    //     else if (rotation_progress < 0 && rotation_progress >= -1)
    //     {
    //         rotation_progress += Time.deltaTime * speed * 10;
    //         rotation_progress =  Math.Min(0.0f, rotation_progress);
    //         start_rotation = body.transform.rotation;
    //         end_rotation = Quaternion.Euler(0, start_rotation.eulerAngles.y + rand_deg, 0);
    //     }
        
    //     else    
    //     {
    //         rotation_progress = -1;
    //     }
    // }

    void MouseControl()
    {
        float rotY = Input.GetAxis("Mouse X");
        transform.Rotate(0.0f, rotY, 0.0f);
    }

}
