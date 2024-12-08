/*
 * Copyright 2021 Google LLC
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.InputSystem;

public class PackageSpawner : MonoBehaviour
{
    public DrivingSurfaceManager DrivingSurfaceManager;
    public PackageBehaviour Package;
    public GameObject PackagePrefab;
    public GameObject AlternatePackagePrefab;
    
    public ParticleSystem TransitionEffectPrefab;
    private bool isPackageTransitioning = false;
    
    public static Vector3 RandomInTriangle(Vector3 v1, Vector3 v2)
    {
        float u = Random.Range(0.0f, 1.0f);
        float v = Random.Range(0.0f, 1.0f);
        if (v + u > 1)
        {
            v = 1 - v;
            u = 1 - u;
        }

        return (v1 * u) + (v2 * v);
    }

    public static Vector3 FindRandomLocation(ARPlane plane)
    {
        // Select random triangle in Mesh
        var mesh = plane.GetComponent<ARPlaneMeshVisualizer>().mesh;
        var triangles = mesh.triangles;
        var triangle = triangles[(int)Random.Range(0, triangles.Length - 1)] / 3 * 3;
        var vertices = mesh.vertices;
        var randomInTriangle = RandomInTriangle(vertices[triangle], vertices[triangle + 1]);
        var randomPoint = plane.transform.TransformPoint(randomInTriangle);

        return randomPoint;
    }

    public void SpawnPackage(ARPlane plane)
    {
        var packageClone = GameObject.Instantiate(PackagePrefab);
        packageClone.transform.position = FindRandomLocation(plane);

        Package = packageClone.GetComponent<PackageBehaviour>();
    }

    // appearance change
    
    private void TryChangeAppearance()
    {
        var touchPosition = Touchscreen.current.primaryTouch.position.ReadValue();
        var ray = Camera.main.ScreenPointToRay(touchPosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject == Package.gameObject)
            {
                if (PackagePrefab != AlternatePackagePrefab && 
                    Package.gameObject.name.StartsWith(AlternatePackagePrefab.name))
                {
                    return;
                }
                
                ReplacePackagePrefab();
            }
        }
    }
    
    private float TRANSITION_DURATION = 0.75f;
        
    private void ReplacePackagePrefab()
    {
        if (Package != null)
        {
            if (TransitionEffectPrefab != null)
            {
                var effect = Instantiate(TransitionEffectPrefab, Package.transform.position, Quaternion.identity);
                Destroy(effect.gameObject, TRANSITION_DURATION);
            }

            isPackageTransitioning = true;
            var packageClone = Instantiate(AlternatePackagePrefab);
            packageClone.transform.position = Package.transform.position;
            
            StartCoroutine(SmoothReplacePackage(Package.gameObject, packageClone));
            
            StartCoroutine(
                SmoothFadeParticleAndSound(TransitionEffectPrefab, TransitionEffectPrefab.GetComponent<AudioSource>())
            );
            
            Package = packageClone.GetComponent<PackageBehaviour>();
            isPackageTransitioning = false;
        }
    }
    
    // transition
    
    private IEnumerator SmoothReplacePackage(GameObject oldPackage, GameObject newPackage)
    {
        float elapsed = 0;

        Vector3 oldPackageOriginalScale = oldPackage.transform.localScale;
        Vector3 oldPackageTargetScale = Vector3.zero; // Shrinks to zero

        Vector3 newPackageTargetScale = Vector3.one; // Final scale (1, 1, 1)
        newPackage.transform.localScale = Vector3.zero; // Start at scale zero

        while (elapsed < TRANSITION_DURATION)
        {
            float t = elapsed / TRANSITION_DURATION; // Normalized time (0 to 1)
            
            oldPackage.transform.localScale = Vector3.Lerp(oldPackageOriginalScale, oldPackageTargetScale, t);
            newPackage.transform.localScale = Vector3.Lerp(Vector3.zero, newPackageTargetScale, t);

            elapsed += Time.deltaTime;
            yield return null;
        }
        
        oldPackage.transform.localScale = oldPackageTargetScale;
        newPackage.transform.localScale = newPackageTargetScale;
        
        Destroy(oldPackage);
    }
    
    private IEnumerator SmoothFadeParticleAndSound(ParticleSystem particleSystem, AudioSource audioSource)
    {
        float elapsed = 0;
        
        ParticleSystem.EmissionModule emission = particleSystem.emission;
        float initialRate = emission.rateOverTime.constant;
        
        float initialVolume = 0;
        if (audioSource != null)
        {
            initialVolume = audioSource.volume;
        }

        while (elapsed < TRANSITION_DURATION)
        {
            float t = elapsed / TRANSITION_DURATION; // Normalized time (0 to 1)
            
            emission.rateOverTime = Mathf.Lerp(initialRate, 0, t);
            if (audioSource != null)
            {
               audioSource.volume = Mathf.Lerp(initialVolume, 0, t);
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        emission.rateOverTime = 0;
        if (audioSource != null)
        {
            audioSource.volume = 0;
        }
        
        particleSystem.Stop();
        
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
    
    private void Update()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
        {
            TryChangeAppearance();
            return;
        }
        
        var lockedPlane = DrivingSurfaceManager.LockedPlane;
        if (lockedPlane != null)
        {
            if (Package == null && !isPackageTransitioning)
            {
                SpawnPackage(lockedPlane);
            }

            var packagePosition = Package.gameObject.transform.position;
            packagePosition.Set(packagePosition.x, lockedPlane.center.y, packagePosition.z);
        }
    }
}
