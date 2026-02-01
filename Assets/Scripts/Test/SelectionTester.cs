using Core;
using Managers;
using UnityEngine;

public class SelectionTester : MonoBehaviour
{
    
    EventManager eventManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        eventManager = Services.Get<EventManager>();
        eventManager.gameObjectSelected += (Transform selected) =>
        {
            Debug.Log($"Selected object: {selected.name}");
        };
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
