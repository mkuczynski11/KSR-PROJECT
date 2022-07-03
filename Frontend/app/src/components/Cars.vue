<template>
  <b-container>
    <b-row v-if="!detailed">
      <b-col cols="5" />
      <b-col cols="2">
        <b-button variant="success" v-b-modal.create-car-modal>Create</b-button>
      </b-col>
      <b-col cols="5" />
    </b-row>
    <b-row v-if="!detailed && carsLoaded" class="py-4">
      <b-col cols="3"></b-col>
      <b-col>
        <b-table
          cols="6"
          v-if="carsLoaded"
          :fields="detailed_fields"
          :items="items"
          show-empty
        >
          <template #cell(name)="data">
            {{ data.item.name }}
          </template>
          <template #cell(details)="row">
            <b-button variant="secondary" @click="row.toggleDetails()">{{
              row.item._showDetails ? "Hide" : "Show"
            }}</b-button>
          </template>
          <template #cell(edit)="data">
            <b-button variant="warning" @click="openEditCarForm(data.item.id)"
              >Edit</b-button
            >
          </template>
          <template #cell(delete)="data">
            <b-button variant="danger" @click="deleteCar(data.item)">
              Delete
            </b-button>
          </template>
          <template #empty="">
            <h4><b>No cars found</b></h4>
          </template>
          <template #row-details="row">
            <b-container>
              <b-row class="mb-2">
                <b-col sm="3" class="text-sm-right"><b>Max speed:</b></b-col>
                <b-col cols="3">{{ row.item.maxSpeed }}</b-col>
                <b-col sm="3" class="text-sm-right"><b>Seats:</b></b-col>
                <b-col cols="3">{{ row.item.seats }}</b-col>
              </b-row>
              <b-row class="mb-2">
                <b-col sm="3" class="text-sm-right"><b>Horsepower:</b></b-col>
                <b-col cols="3">{{ row.item.horsePower }}</b-col>
                <b-col sm="3" class="text-sm-right"><b>Displacement:</b></b-col>
                <b-col cols="3">{{ row.item.displacement }}</b-col>
              </b-row>
              <b-row class="mb-2">
                <b-col sm="3" class="text-sm-right"><b>Wheels:</b></b-col>
                <b-col cols="3">{{ row.item.wheels }}</b-col>
                <b-col sm="3" class="text-sm-right"><b>Doors:</b></b-col>
                <b-col cols="3">{{ row.item.doors }}</b-col>
              </b-row>
              <b-row class="mb-2">
                <b-col sm="3" class="text-sm-right"><b>User:</b></b-col>
                <b-col cols="3">{{ row.item.user }}</b-col>
              </b-row>
            </b-container>
          </template>
        </b-table>
      </b-col>
      <b-col cols="3"></b-col>
    </b-row>
    <b-row v-if="detailed && carsLoaded">
      <b-col>
        <b-table cols="6" :fields="general_fields" :items="items" show-empty>
          <template #cell(max_speed)="data">
            {{ data.item.maxSpeed }}
          </template>
          <template #cell(displacement)="data">
            {{ data.item.displacement }}
          </template>
          <template #cell(horsepower)="data">
            {{ data.item.horsePower }}
          </template>
          <template #cell(delete)="data">
            <b-button variant="danger" @click="deleteCar(data.item)">
              Delete
            </b-button>
          </template>
          <template #empty="">
            <h4><b>No cars found</b></h4>
          </template>
        </b-table>
      </b-col>
    </b-row>
    <b-modal id="create-car-modal" size="lg" :hide-header="true">
      <b-container>
        <h4>Create new user form</h4>
        <hr />
        <b-row class="pt-2">
          <b-col cols="3">
            <b>Name</b>
          </b-col>
          <b-col cols="3">
            <b-form-input v-model="form.name" placeholder="Name" />
          </b-col>
          <b-col cols="3">
            <b>Max speed</b>
          </b-col>
          <b-col cols="3">
            <b-form-input v-model="form.maxSpeed" placeholder="Max speed" />
          </b-col>
        </b-row>
        <b-row class="pt-4">
          <b-col cols="3">
            <b>Horsepower</b>
          </b-col>
          <b-col cols="3">
            <b-form-input v-model="form.horsePower" placeholder="Horsepower" />
          </b-col>
          <b-col cols="3">
            <b>Displacement</b>
          </b-col>
          <b-col cols="3">
            <b-form-input
              v-model="form.displacement"
              placeholder="Displacement"
            />
          </b-col>
        </b-row>
        <b-row class="pt-4">
          <b-col cols="3">
            <b>Seats</b>
          </b-col>
          <b-col cols="3">
            <b-form-input v-model="form.seats" placeholder="Seats" />
          </b-col>
          <b-col cols="3">
            <b>Wheels</b>
          </b-col>
          <b-col cols="3">
            <b-form-input v-model="form.wheels" placeholder="Wheels" />
          </b-col>
        </b-row>
        <b-row class="pt-4">
          <b-col cols="3">
            <b>Doors</b>
          </b-col>
          <b-col cols="3">
            <b-form-input v-model="form.doors" placeholder="Doors" />
          </b-col>
          <b-col cols="3">
            <b>User</b>
          </b-col>
          <b-col cols="3">
            <b-form-input v-model="form.user" placeholder="User login" />
          </b-col>
        </b-row>
      </b-container>
      <template #modal-footer="{}">
        <b-button variant="danger" @click="closeCreateCarForm()">
          Cancel
        </b-button>
        <b-button variant="success" @click="createCar()">Create car</b-button>
      </template>
    </b-modal>
    <b-modal id="edit-car-modal" size="lg" :hide-header="true">
      <b-container>
        <h4>Edit car form</h4>
        <hr />
        <b-row class="pt-2">
          <b-col cols="3">
            <b>Max speed</b>
          </b-col>
          <b-col cols="3">
            <b-form-input
              v-model="carToEdit.maxSpeed"
              placeholder="Max speed"
            />
          </b-col>
          <b-col cols="3">
            <b>Seats</b>
          </b-col>
          <b-col cols="3">
            <b-form-input v-model="carToEdit.seats" placeholder="Seats" />
          </b-col>
        </b-row>
        <b-row class="pt-2">
          <b-col cols="3">
            <b>Horsepower</b>
          </b-col>
          <b-col cols="3">
            <b-form-input
              v-model="carToEdit.horsePower"
              placeholder="Horsepower"
            />
          </b-col>
        </b-row>
      </b-container>
      <template #modal-footer="{}">
        <b-button variant="danger" @click="closeEditCarForm()">
          Cancel
        </b-button>
        <b-button variant="success" @click="editCar()">Edit car</b-button>
      </template>
    </b-modal>
  </b-container>
</template>

<script>
import {
  getCars,
  getCar,
  getUserCars,
  deleteCar as apiDeleteCar,
  createCar as apiCreateCar,
  editCar as apiEditCar,
} from "@/api/api.js";

const DETAILED_FIELDS = ["name", "details", "edit", "delete"];

const GENERAL_FIELDS = [
  "name",
  "max_speed",
  "displacement",
  "horsepower",
  "delete",
];

export default {
  name: "Cars",

  props: {
    detailed: Boolean,
    user: String,
  },

  data: function () {
    return {
      cars: [],
      items: [],
      general_fields: GENERAL_FIELDS,
      detailed_fields: DETAILED_FIELDS,
      carsLoaded: false,
      carsLoadedDetailed: false,
      carLoading: false,
      nextIndex: 2,
      form: {
        id: null,
        name: null,
        maxSpeed: null,
        horsePower: null,
        displacement: null,
        seats: null,
        doors: null,
        wheels: null,
        user: null,
      },
      carToEdit: {
        maxSpeed: null,
        horsePower: null,
        seats: null,
      },
    };
  },
  async mounted() {
    await this.parseCars();
  },

  methods: {
    async loadCars() {
      let res = null;
      if (this.detailed) {
        res = await getUserCars(this.user);
      } else {
        res = await getCars();
      }

      this.cars = res.cars;
    },

    async parseCars() {
      this.carsLoaded = false;
      await this.loadCars();
      this.items = [];

      for (let car of this.cars) {
        let detailedCar = await getCar(car.id);
        this.items.push(detailedCar);
        this.nextIndex++;
      }

      this.carsLoaded = true;
    },

    async editCar() {
      await apiEditCar(this.carToEdit);
      this.resetEditForm();
      this.parseCars();
      this.closeEditCarForm();
    },

    openEditCarForm(id) {
      this.findCarToEdit(id);
      this.$bvModal.show("edit-car-modal");
    },

    async findCarToEdit(id) {
      for (const car of this.items) {
        if (car.id == id) {
          console.log(car);
          this.carToEdit = car;
          break;
        }
      }
    },

    async deleteCar(car) {
      await apiDeleteCar(car.id);
      this.parseCars();
    },

    async createCar() {
      console.log(this.form);
      this.form.id = this.nextIndex;
      this.nextIndex++;
      await apiCreateCar(this.form);
      this.resetCreateForm();
      this.parseCars();
      this.closeCreateCarForm();
    },

    resetCreateForm() {
      for (let key in this.form) {
        this.form[key] = null;
      }
    },

    resetEditForm() {
      for (let key in this.carToEdit) {
        this.form[key] = null;
      }
    },

    closeCreateCarForm() {
      this.$bvModal.hide("create-car-modal");
    },

    closeEditCarForm() {
      this.$bvModal.hide("edit-car-modal");
    },
  },
};
</script>

<style lang="scss" scoped></style>
