using ARMeilleure.Memory;
using Ryujinx.Memory;
using Ryujinx.Memory.Tracking;
using System;

namespace Ryujinx.Cpu.Nce
{
    /// <summary>
    /// Represents a CPU memory manager which maps guest virtual memory directly onto a host virtual region.
    /// </summary>
    public sealed class MemoryManagerNative : MemoryManagerSoftware, ICpuMemoryManager, IVirtualMemoryManagerTracked, IWritableBlock
    {
        private readonly InvalidAccessHandler _invalidAccessHandler;

        private readonly MemoryBlock _addressSpace;
        private readonly ulong _addressSpaceSize;

        private readonly MemoryBlock _backingMemory;

        private readonly MemoryEhMeilleure _memoryEh;

        /// <inheritdoc/>
        public bool Supports4KBPages => MemoryBlock.GetPageSize() == PageSize;

        public IntPtr PageTablePointer => IntPtr.Zero;

        public ulong ReservedSize => (ulong)_addressSpace.Pointer.ToInt64();

        public MemoryManagerType Type => MemoryManagerType.HostMappedUnsafe;

        public event Action<ulong, ulong> UnmapEvent;

        /// <summary>
        /// Creates a new instance of the host mapped memory manager.
        /// </summary>
        /// <param name="addressSpace">Address space memory block</param>
        /// <param name="backingMemory">Physical backing memory where virtual memory will be mapped to</param>
        /// <param name="addressSpaceSize">Size of the address space</param>
        /// <param name="invalidAccessHandler">Optional function to handle invalid memory accesses</param>
        public MemoryManagerNative(
            MemoryBlock addressSpace,
            MemoryBlock backingMemory,
            ulong addressSpaceSize,
            InvalidAccessHandler invalidAccessHandler = null) : base(backingMemory, addressSpaceSize, invalidAccessHandler)
        {
            _backingMemory = backingMemory;
            _invalidAccessHandler = invalidAccessHandler;
            _addressSpaceSize = addressSpaceSize;
            _addressSpace = addressSpace;

            Tracking = new MemoryTracking(this, PageSize, invalidAccessHandler);
            _memoryEh = new MemoryEhMeilleure(addressSpaceSize, Tracking);
        }

        /// <inheritdoc/>
        public void Map(ulong va, ulong pa, ulong size, MemoryMapFlags flags)
        {
            AssertValidAddressAndSize(va, size);

            _addressSpace.MapView(_backingMemory, pa, AddressToOffset(va), size);
            AddMapping(va, size);
            PtMap(va, pa, size);

            Tracking.Map(va, size);
        }

        /// <inheritdoc/>
        public void MapForeign(ulong va, nuint hostPointer, ulong size)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public void Unmap(ulong va, ulong size)
        {
            AssertValidAddressAndSize(va, size);

            UnmapEvent?.Invoke(va, size);
            Tracking.Unmap(va, size);

            RemoveMapping(va, size);
            PtUnmap(va, size);
            _addressSpace.UnmapView(_backingMemory, AddressToOffset(va), size);
        }

        /// <inheritdoc/>
        public void Reprotect(ulong va, ulong size, MemoryPermission permission)
        {
            _addressSpace.Reprotect(AddressToOffset(va), size, permission);
        }

        /// <inheritdoc/>
        public override void TrackingReprotect(ulong va, ulong size, MemoryPermission protection)
        {
            base.TrackingReprotect(va, size, protection);

            _addressSpace.Reprotect(AddressToOffset(va), size, protection, false);
        }

        private ulong AddressToOffset(ulong address)
        {
            if (address < ReservedSize)
            {
                throw new ArgumentException($"Invalid address 0x{address:x16}");
            }

            return address - ReservedSize;
        }

        /// <summary>
        /// Disposes of resources used by the memory manager.
        /// </summary>
        protected override void Destroy()
        {
            _addressSpace.Dispose();
            _memoryEh.Dispose();
        }
    }
}
