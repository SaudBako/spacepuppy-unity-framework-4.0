﻿//
// IMemberAccessor.cs
//
// Author: James Nies
// Licensed under The Code Project Open License (CPOL): http://www.codeproject.com/info/cpol10.aspx

namespace com.spacepuppy.Dynamic.Accessors
{
    /// <summary>
    /// The IMemberAccessor interface defines a member
    /// accessor.
    /// </summary>
    public interface IMemberAccessor
    {

        string GetMemberName();

        /// <summary>
        /// Return the member type that Get/Set expects.
        /// </summary>
        /// <returns></returns>
        System.Type GetMemberType();

        /// <summary>
        /// Gets the value stored in the member for
        /// the specified target.
        /// </summary>
        /// <param name="target">Object to retrieve
        /// the member from.</param>
        /// <returns>Member value.</returns>
        object Get(object target);

        /// <summary>
        /// Sets the value for the member of
        /// the specified target.
        /// </summary>
        /// <param name="target">Object to set the
        /// member on.</param>
        /// <param name="value">Member value.</param>
        void Set(object target, object value);
    }
    
    public interface IMemberAccessor<T> : IMemberAccessor
    {
        T Get(object target);
        void Set(object target, T value);
    }

}