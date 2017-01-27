using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommonModule.Commands;
using CommonModule.Interfaces;
using DataObjects;
using DataObjects.Interfaces;
using CommonModule.Helpers;

namespace CommonModule.ViewModels
{
    public class ProductSelectionViewModel : BaseDlgViewModel
    {
        private IDbService repository;

        public ProductSelectionViewModel(IDbService _rep)
            : this(_rep, Enumerable.Empty<ProductInfo>())
        {
        }

        public ProductSelectionViewModel(IDbService _rep, IEnumerable<ProductInfo> _productlist)
        {
            repository = _rep;
            productList = new ObservableCollection<Selectable<ProductInfo>>(_productlist.Select(p => new Selectable<ProductInfo>(p)));
            if (IsFiltered) DoFilterProducts();                
            Title = "Выбор продукта/услуги";            
        }

        public override bool IsValid()
        {
            return base.IsValid()
                && SelectedProduct != null;
        }

        public bool IsMultiSelect { get; set; }

        private string selectedProductsLabel;
        public string SelectedProductsLabel
        {
            get { return selectedProductsLabel; }
            set { SetAndNotifyProperty("SelectedProductsLabel", ref selectedProductsLabel, value); }
        }

        private ObservableCollection<Selectable<ProductInfo>> productList;
        public ObservableCollection<Selectable<ProductInfo>> ProductList
        {
            get { return productList; }
        }

        public void PopulateProductList(IEnumerable<ProductInfo> _productlist)
        {
            productList.Clear();
            foreach (var p in _productlist)
                productList.Add(new Selectable<ProductInfo>(p));

            if (IsFiltered) DoFilterProducts();                
        }

        private Selectable<ProductInfo> selectedProductItem;
        public Selectable<ProductInfo> SelectedProductItem
        {
            get
            {
                return selectedProductItem;
            }
            set
            {
                SetAndNotifyProperty("SelectedProductItem", ref selectedProductItem, value);
            }
        }

        //private ProductInfo selectedProduct;
        public ProductInfo SelectedProduct
        {
            get
            {
                return selectedProductItem == null ? null : selectedProductItem.Value;
            }
            set
            {
                if (productList != null && productList.Count > 0)
                    SetAndNotifyProperty("SelectedProductItem", ref selectedProductItem, productList.SingleOrDefault(pi => pi.Value == value));
            }
        }

        /// <summary>
        /// шаблон для поиска
        /// </summary>
        private string seekPat;
        public string SeekPat
        {
            get { return seekPat; }
            set
            {
                if (value != seekPat)
                {
                    seekPat = value;
                    NotifyPropertyChanged("SeekPat");
                }

            }
        }

        /// <summary>
        /// Комманда запуска поиска контрагентов
        /// </summary>
        private ICommand seekCommand;
        public ICommand SeekCommand
        {
            get
            {
                if (seekCommand == null)
                    seekCommand = new DelegateCommand(ExecSeekCommand, CanExecSeekCommand);
                return seekCommand;
            }
        }
        private bool CanExecSeekCommand()
        {
            return !String.IsNullOrEmpty(seekPat);
        }
        private void ExecSeekCommand()
        {
            //if (IsValid() && seekKod == SelectedProduct.Kgr)
            //    SubmitCommand.Execute(null);
            //else
            //{
                SeekProduct();
                SelectedProductItem = ProductList.Count > 0 ? ProductList[0] : null;
            //}
        }

        /// <summary>
        /// Ищет продукты по части введённого кода
        /// </summary>
        /// <param name="_number"></param>
        public void SeekProduct()
        {
            IEnumerable<ProductInfo> prlbypat = null;

            if (!String.IsNullOrEmpty(seekPat))
                prlbypat = repository.GetProductsByPat(seekPat);

            PopulateProductList(prlbypat);
        }

        public bool IsFiltered { get; set; }

        private IEnumerable<Selectable<ProductInfo>> filteredProducts;

        public IEnumerable<Selectable<ProductInfo>> FilteredProducts
        {
            get { return filteredProducts; }
        }

        private ICommand filterProductsCommand;

        public ICommand FilterProductsCommand
        {
            get 
            { 
                if (filterProductsCommand == null)
                    filterProductsCommand = new DelegateCommand(DoFilterProducts);
                return filterProductsCommand; 
            }
        }

        private void DoFilterProducts()
        {
            IEnumerable<Selectable<ProductInfo>> prbypat = null;

            if (productList == null || productList.Count == 0)
                prbypat = Enumerable.Empty<Selectable<ProductInfo>>();
            else
                if (String.IsNullOrEmpty(seekPat))
                    prbypat = productList;
                else
                {
                    int l_kpr = 0;
                    if (int.TryParse(seekPat, out l_kpr))
                        prbypat = productList.Where(sp => sp.Value.Kpr == l_kpr);
                    else
                        prbypat = productList.Where(sp => sp.Value.Name.Contains(seekPat));
                }

            SetAndNotifyProperty("FilteredProducts", ref filteredProducts, prbypat);

            if (selectedProductItem == null || !prbypat.Contains(selectedProductItem))
                SetAndNotifyProperty("SelectedProductItem", ref selectedProductItem, filteredProducts.FirstOrDefault());
        }

    }
}
