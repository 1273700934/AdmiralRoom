﻿using System;
using Meowtrix;
using Meowtrix.ComponentModel;

namespace Huoyaoyuan.AdmiralRoom.Officer
{
    public abstract class GameObject<T> : NotificationObject, IUpdatable<T>, IIdentifiable<int>, IComparable<GameObject<T>>
    {
        protected T rawdata;
        protected GameObject(T data)
        {
            Update(data);
        }
        public void Update(T data)
        {
            rawdata = data;
            UpdateProp();
            OnAllPropertyChanged();
        }
        protected virtual void UpdateProp() { }
        public void Update()
        {
            UpdateProp();
            OnAllPropertyChanged();
        }
        public abstract int Id { get; }
        public int CompareTo(GameObject<T> other) => Id - other.Id;
    }
}
