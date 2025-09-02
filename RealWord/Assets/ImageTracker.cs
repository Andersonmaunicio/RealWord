using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Garante que este script só pode ser adicionado a um objeto que já tenha o ARTrackedImageManager
[RequireComponent(typeof(ARTrackedImageManager))]
public class ImageTracker : MonoBehaviour
{
    // Variável para guardar a referência ao nosso gerenciador de imagens
    private ARTrackedImageManager trackedImageManager;

    // Esta estrutura nos permite organizar no Inspector do Unity
    // qual nome de imagem corresponde a qual objeto 3D (Prefab)
    [System.Serializable]
    public struct TrackedImagePrefab
    {
        public string imageName; // O nome que demos à imagem na Biblioteca (ex: "Maca")
        public GameObject prefab;  // O Prefab que queremos que apareça
    }

    // Uma lista pública para arrastarmos nossos prefabs no Inspector
    public List<TrackedImagePrefab> trackedImagePrefabs;

    // Um dicionário para guardar os objetos que já foram criados, para não criá-los de novo
    private Dictionary<string, GameObject> instantiatedPrefabs = new Dictionary<string, GameObject>();

    void Awake()
    {
        // Pega o componente ARTrackedImageManager que está no mesmo objeto deste script
        trackedImageManager = GetComponent<ARTrackedImageManager>();
    }

    void OnEnable()
    {
        // Se inscreve no evento. Toda vez que uma imagem for detectada/perdida, o método OnTrackedImagesChanged será chamado
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        // Cancela a inscrição no evento para evitar erros quando o objeto for desativado
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    // Este é o método principal, chamado pelo AR Foundation sempre que o status de uma imagem muda
    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // Itera por todas as imagens que foram atualizadas nesta frame
        foreach (var trackedImage in eventArgs.updated)
        {
            var imageName = trackedImage.referenceImage.name;
            GameObject currentPrefab = instantiatedPrefabs.GetValueOrDefault(imageName);

            // Se a imagem está sendo rastreada (visível e estável)
            if (trackedImage.trackingState == TrackingState.Tracking)
            {
                // Se o prefab correspondente ainda não foi criado
                if (currentPrefab == null)
                {
                    // Procura na nossa lista pública qual prefab corresponde ao nome da imagem detectada
                    foreach (var trackedPrefab in trackedImagePrefabs)
                    {
                        if (trackedPrefab.imageName == imageName)
                        {
                            // Cria uma nova instância do prefab na mesma posição e rotação da imagem
                            // e o torna "filho" da imagem, para que se mova junto com ela
                            GameObject newPrefab = Instantiate(trackedPrefab.prefab, trackedImage.transform);
                            // Guarda o prefab recém-criado no nosso dicionário
                            instantiatedPrefabs[imageName] = newPrefab;
                        }
                    }
                }
                else // Se o prefab já existe (estava apenas desativado)
                {
                    // Reativa o prefab e atualiza sua posição e rotação
                    currentPrefab.SetActive(true);
                    currentPrefab.transform.SetPositionAndRotation(trackedImage.transform.position, trackedImage.transform.rotation);
                }
            }
            else // Se a imagem foi perdida ou não está sendo rastreada com clareza
            {
                // Se o prefab correspondente existe
                if (currentPrefab != null)
                {
                    // Desativa o prefab em vez de destruí-lo. Isso é mais eficiente.
                    currentPrefab.SetActive(false);
                }
            }
        }
    }
}