﻿using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public partial class Product : BaseEntity
    {

        [ObservableProperty]
        public string? _id;

        [ObservableProperty]
        public string? _name;

        [ObservableProperty]
        public string? _description;

        [ObservableProperty]
        public string? _category;

        [ObservableProperty]
        public string? _purity;

        [ObservableProperty]
        public decimal? _purityPercent;

        [ObservableProperty]
        public string? _imageRef;

        [ObservableProperty]
        public int? _setGkey;

        [ObservableProperty]
        public string? _stockGroup;

        [ObservableProperty]
        public string? _brand;

        [ObservableProperty]
        public string? _metal;

        [ObservableProperty]
        public string? _hsnCode;

        [ObservableProperty]
        public string? _model;

        [ObservableProperty]
        public string? _uom;

        [ObservableProperty]
        public string? _taxRule;

        [ObservableProperty]
        public string? _baseUnit;

        [ObservableProperty]
        public bool? _isTaxable;

        [ObservableProperty]
        public bool? _isActive;

        [ObservableProperty]
        public int? _productAttributeGkey;

        [ObservableProperty]
        public string? _createdBy;

        [ObservableProperty]
        public DateTime? _createdOn;

        [ObservableProperty]
        public string? _modifiedBy;

        [ObservableProperty]
        public DateTime? _modifiedOn;

    }
}
