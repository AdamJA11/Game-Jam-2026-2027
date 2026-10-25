using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Transition : MonoBehaviour
{
    public Transform lineEnd; 

    public Transform mainTransform; 
    public Transform transorm1; 
    public Transform transorm2; 
    
    public float speed; 
    public float stopDistance;

    private Vector3 startPosMain;
    private Vector3 startPos1;
    private Vector3 startPos2;

    private bool isTransitioning = false;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        startPosMain = mainTransform.position;
        startPos1 = transorm1.position;
        startPos2 = transorm2.position;
    }

    void OnEnable()
    {
        isTransitioning = false;
    }

    void Update()
    {
        if (!isTransitioning)
        {
            // Calcul des cibles sur la ligne horizontale
            Vector3 targetMain = new Vector3(mainTransform.position.x, lineEnd.position.y, mainTransform.position.z);
            Vector3 target1    = new Vector3(transorm1.position.x, lineEnd.position.y, transorm1.position.z);
            Vector3 target2    = new Vector3(transorm2.position.x, lineEnd.position.y, transorm2.position.z);

            // Déplacement vers le centre
            mainTransform.position = Vector3.MoveTowards(mainTransform.position, targetMain, speed * Time.deltaTime);
            transorm1.position     = Vector3.MoveTowards(transorm1.position, target1, speed * Time.deltaTime);
            transorm2.position     = Vector3.MoveTowards(transorm2.position, target2, speed * Time.deltaTime);

            // Vérifie si les 3 bannières ont atteint la ligne
            bool mainReached = Vector3.Distance(mainTransform.position, targetMain) <= stopDistance + 0.01f;
            bool t1Reached   = Vector3.Distance(transorm1.position, target1) <= stopDistance + 0.01f;
            bool t2Reached   = Vector3.Distance(transorm2.position, target2) <= stopDistance + 0.01f;

            if (mainReached && t1Reached && t2Reached)
            {
                isTransitioning = true;
                // Lance le chargement en arrière-plan et gère le timing du retour
                StartCoroutine(LoadSceneAndReturnRoutine());
            }
        }
        else
        {
            // Retour vers les positions de départ (dans la nouvelle scène)
            mainTransform.position = Vector3.MoveTowards(mainTransform.position, startPosMain, speed * Time.deltaTime);
            transorm1.position     = Vector3.MoveTowards(transorm1.position, startPos1, speed * Time.deltaTime);
            transorm2.position     = Vector3.MoveTowards(transorm2.position, startPos2, speed * Time.deltaTime);

            bool mainBack = Vector3.Distance(mainTransform.position, startPosMain) <= 0.01f;
            bool t1Back   = Vector3.Distance(transorm1.position, startPos1) <= 0.01f;
            bool t2Back   = Vector3.Distance(transorm2.position, startPos2) <= 0.01f;

            if (mainBack && t1Back && t2Back)
            {
                // La transition est totalement terminée, on désactive l'objet
                gameObject.SetActive(false);
            }
        }
    }

    private IEnumerator LoadSceneAndReturnRoutine()
    {
        // Charge la scène suivante automatiquement (index actuel + 1) en arrière-plan
        //AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex + 1);
        
        // Attend un tout petit instant pour s'assurer que l'écran est bien 100% couvert
        yield return new WaitForSeconds(0.15f);

        // Le code sort d'ici et l'Update() s'occupe de faire repartir les bannières en sens inverse
    }
}