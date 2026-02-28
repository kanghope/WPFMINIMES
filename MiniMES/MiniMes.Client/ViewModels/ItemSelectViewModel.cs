using MiniMes.Client.Helpers;
using MiniMes.Domain.DTOs;
using MiniMES.Infastructure.interfaces;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace MiniMes.Client.ViewModels
{
    public class ItemSelectViewModel : BaseViewModel
    {
        private readonly IItemService _itemService;
        private string _searchText;
        private ItemDto _selectedItem;

        // 원본 리스트와 화면에 보여줄 필터링된 리스트
        public ObservableCollection<ItemDto> AllItems { get; set; } = new();
        public ObservableCollection<ItemDto> FilteredItems { get; set; } = new();

        // 사용자가 선택한 아이템
        public ItemDto SelectedItem
        {
            get => _selectedItem;
            set { _selectedItem = value; OnPropertyChanged(nameof(SelectedItem)); }
        }

        // 검색어
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(nameof(SearchText)); }
        }

        // 커맨드
        public ICommand SearchCommand { get; }
        public ICommand SelectCommand { get; }

        // 부모 창에서 결과를 확인하기 위한 Action (창 닫기용)
        public System.Action RequestClose { get; set; }

        public ItemSelectViewModel(IItemService itemService)
        {
            _itemService = itemService;
            SearchCommand = new RelayCommand(ExecuteSearch);
            SelectCommand = new RelayCommand(ExecuteSelect);

            LoadInitialData();
        }

        private async void LoadInitialData()
        {
            // 1. 모든 품목 마스터 로드
            var list = await _itemService.GetAllItemsAsync();

            // 2. 자재(RM)와 반제품(SG)만 필터링해서 담기 (완제품은 제외)
            var targetItems = list.Where(x => x.ItemType == "RM" || x.ItemType == "SG");

            AllItems.Clear();
            foreach (var item in targetItems)
            {
                AllItems.Add(item);
                FilteredItems.Add(item);
            }
        }

        private void ExecuteSearch()
        {
            // 검색어가 포함된 항목만 필터링
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredItems = new ObservableCollection<ItemDto>(AllItems);
            }
            else
            {
                var filtered = AllItems.Where(x =>
                    x.ItemCode.Contains(SearchText.ToUpper()) ||
                    x.ItemName.Contains(SearchText));

                FilteredItems.Clear();
                foreach (var item in filtered) FilteredItems.Add(item);
            }
            OnPropertyChanged(nameof(FilteredItems));
        }

        private void ExecuteSelect()
        {
            if (SelectedItem != null)
            {
                // 선택된 아이템이 있으면 창을 닫도록 신호 보냄
                RequestClose?.Invoke();
            }
        }
    }
}