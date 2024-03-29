//using System.Collections;
//using System.Collections.Generic;
//using Unity.Netcode;
//using UnityEngine;

//public class VillageView : MonoBehaviour
//{
//    [SerializeField] private GameObject _textField;

//    private VillageViewModel _viewModel;

//    public void Init(VillageViewModel viewModel) {
//        _viewModel = viewModel;

//        _viewModel.CounterView.OnChanged += DisplayCounter;
//    }

//    [ClientRpc]
//    private void DisplayCounter(int value) {

//    }
//}
