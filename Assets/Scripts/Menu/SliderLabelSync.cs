using UnityEngine;
using UnityEngine.UI;

// Pri načítaní uložených hodnôt (SetValueWithoutNotify) sa onValueChanged nevolá –
// raz ho vyvolá, aby sa obnovil text "Volume: 70%" a ikona reproduktora, a potom sa odstráni.
public class SliderLabelSync : MonoBehaviour
{
    private Slider _slider;

    public void Init(Slider slider) => _slider = slider;

    private void Update()
    {
        if (_slider != null)
            _slider.onValueChanged.Invoke(_slider.value);
        Destroy(this);
    }
}
