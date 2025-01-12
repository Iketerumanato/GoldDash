using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//色選択のボタンだけグレーアウトの方法が特殊なので専用のスクリプトを用意した
public class ColorSelectButtonColorChanger : MonoBehaviour
{
    //色を変えたい矢印
    [SerializeField] private Image m_arrowLeftUpper;
    [SerializeField] private Image m_arrowRightUpper;
    [SerializeField] private Image m_arrowLeftLower;
    [SerializeField] private Image m_arrowRightLower;
    //色を変えたいテキスト
    [SerializeField] private TextMeshProUGUI m_upperText;
    [SerializeField] private TextMeshProUGUI m_lowerText;
    //色指定
    [SerializeField] private Color m_unselectedColor; //選択されていない時の色
    [SerializeField] private Color m_selectedColor; //選択時の色

    //選択状態であるか　外部から変更するためのプロパティ
    private bool m_isSelected;
    public bool IsSelected
    {
        set
        {
            m_isSelected = value;
            ChangeColor(m_isSelected);
        }

        get { return m_isSelected; }
    }

    //選択状態に応じてイメージとテキストの色を変更する
    private void ChangeColor(bool isSelected)
    {
        if (isSelected)
        {
            m_arrowLeftUpper.color = m_selectedColor;
            m_arrowRightUpper.color = m_selectedColor;
            m_arrowLeftLower.color = m_selectedColor;
            m_arrowRightLower.color = m_selectedColor;
            m_upperText.color = m_selectedColor;
            m_lowerText.color = m_selectedColor;
        }
        else
        {
            m_arrowLeftUpper.color = m_unselectedColor;
            m_arrowRightUpper.color = m_unselectedColor;
            m_arrowLeftLower.color = m_unselectedColor;
            m_arrowRightLower.color = m_unselectedColor;
            m_upperText.color = m_unselectedColor;
            m_lowerText.color = m_unselectedColor;
        }
    }
}
