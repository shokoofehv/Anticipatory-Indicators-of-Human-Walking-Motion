using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Text;


public class Target1 : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // double[] x = {3, 1, 2, 2, 1};
        // double[] y = {2, 0, 0, 3, 3, 1, 0};
        // SimpleDTW dtw = new SimpleDTW(x,y);
        // dtw.computeDTW();
        // double[,] f = dtw.getFMatrix();
        // List<List<int>> path = new List<List<int>>();
        // int i =1;
        // int j =1;
        // while (i != 0 && j != 0)
        // {   
        //     i = x.Length - 1;
        //     j = y.Length - 1;
        //     path.Add(new List<int> {i, j});
            
        //     if (f[i-1, j] < f[i-1, j-1] && f[i-1, j] < f[i, j-1])
        //         i -= 1;
        //     else if (f[i, j-1] < f[i-1, j-1] && f[i, j-1] < f[i-1, j])
        //         j -= 1;
        //     else 
        //     {
        //         i -= 1;
        //         j -= 1;
        //     }
        // }
        // foreach (var p in path)
        //     Debug.Log("i: {0}, j: {1}"+ p[0].ToString() + p[1].ToString());

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
