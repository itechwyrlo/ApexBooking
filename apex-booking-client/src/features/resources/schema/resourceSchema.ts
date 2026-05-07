// import type { ModelSchema } from "../../../components/ui/table/types";
// import type { Resource } from "../types";


// const EMPTY_FORM: CreateResourceRequest = {
//   name: '',
//   resourceType: 'Person',
//   capacity: 1,
//   description: '',
// };

// export const resourceFormSchema: ModelSchema<CreateResourceRequest>[] = [
//   { key: 'name', label: 'Name', type: 'string', required: true },
//   {
//     key: 'resourceType',
//     label: 'Type',
//     type: 'select',
//     required: true,
//     options: [
//       { label: 'Person', value: 'Person' },
//       { label: 'Room', value: 'Room' },
//       { label: 'Equipment', value: 'Equipment' },
//       { label: 'Slot Based', value: 'SlotBased' },
//     ],
//   },
//   { key: 'capacity', label: 'Capacity', type: 'number', required: true },
//   { key: 'description', label: 'Description', type: 'textarea' },
// ];